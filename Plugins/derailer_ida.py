# Interop between IDA and Derailer
# https://github.com/Gericom/Derailer

from idautils import *
from idaapi import *
from idc import *
import os

# ...\Derailer\Derailer\bin\Debug\
assert "DERAILER_ROOT" in os.environ
DERAILER_PATH = os.getenv("DERAILER_ROOT")

CLANG_FORMAT = "C:\\Program Files\\LLVM\\bin\\clang-format.exe"

import clr
clr.AddReference(os.path.join(DERAILER_PATH, "LibDerailer.dll"))

# Ensure the right capstone dll is loaded
import ctypes
ctypes.cdll.LoadLibrary(os.path.join(DERAILER_PATH, "x64", "capstone.dll"))

from System import Array
from System import Byte
from System import Exception
from LibDerailer.CodeGraph import Decompiler
from LibDerailer.CodeGraph import ProgramContext
from Gee.External.Capstone.Arm import ArmDisassembleMode
from LibDerailer.CCodeGen import CTokenType
from System import Diagnostics

def resolveSymbol(addr):
	name = idc.get_name(addr)
	if name != "" and not name.startswith("loc_"):
		return name
	return None

def makeProgramContext():
	context = ProgramContext()	
	context.ResolveSymbol += resolveSymbol
	return context

def decompile(data, addr):
	if GetReg(addr, "T"):
		mode = ArmDisassembleMode.Thumb
	else:
		mode = ArmDisassembleMode.Arm

	try:
		func = Decompiler.DisassembleArm(Array[Byte](data), addr, mode, makeProgramContext())
	except Exception as e:
		print(e.ToString())
		return ["Derailer failed to decompile the function"]

	tokens = func.CachedMethod.ToTokens()

	result = ""
	for token in tokens:
		colors = {
			CTokenType.Whitespace: idaapi.SCOLOR_INSN,

			CTokenType.Operator:   idaapi.SCOLOR_ALTOP,
			CTokenType.Literal:    idaapi.SCOLOR_REGCMT, # TODO
			CTokenType.Type:       idaapi.SCOLOR_CREFTAIL, # TODO
			CTokenType.Identifier: idaapi.SCOLOR_SYMBOL, # TODO
			CTokenType.Keyword:    idaapi.SCOLOR_KEYWORD,

			CTokenType.OpenBrace:  idaapi.SCOLOR_SYMBOL,
			CTokenType.CloseBrace: idaapi.SCOLOR_SYMBOL,

			CTokenType.OpenParen:  idaapi.SCOLOR_SYMBOL,
			CTokenType.CloseParen: idaapi.SCOLOR_SYMBOL,

			CTokenType.OpenBracket:  idaapi.SCOLOR_SYMBOL,
			CTokenType.CloseBracket: idaapi.SCOLOR_SYMBOL,

			CTokenType.Comma:     idaapi.SCOLOR_SYMBOL,
			CTokenType.Semicolon: idaapi.SCOLOR_SYMBOL,
			CTokenType.Colon:     idaapi.SCOLOR_SYMBOL,

			CTokenType.Cast: idaapi.SCOLOR_DNUM
		}

		result += idaapi.COLSTR(token.Data, colors[token.Type])


	return result.encode("ascii").split('\n')

def get_highlight():
	view = ida_kernwin.get_current_viewer()

	symbol = None
	ret = ida_kernwin.get_highlight(view)
	if ret and ret[1]:
		symbol = ret[0]

	return (view, symbol)

def goto(shift=False):
	view, symbol = get_highlight()

	if not symbol:
		return -1

	address = idaapi.get_name_ea(idc.BADADDR, symbol)
	if not address:
		return -1

	# idaapi.jumpto(address)

	return address

def rename():
	view, symbol = get_highlight()

	if not symbol:
		return False

	new_name = idaapi.askstr(0, 'New Name', 'Rename Function')
	if not new_name or len(new_name) == 0:
		return False

	for letter in new_name:
		if not (letter.isdigit() or letter.isalpha() or letter == '_'):
			print("Invalid identifier")
			return False

	# Duplicate check
	# 	address = idaapi.get_name_ea(idc.BADADDR, new_name)
	# 	if address:
	# 		print("Duplicate\n")
	# 		return False

	address = idaapi.get_name_ea(idc.BADADDR, symbol)
	if not address:
		print("Not a function")
		return False

	idc.MakeName(address, new_name)
	return True

def retype():
	view, symbol = get_highlight()
	address = idaapi.get_name_ea(idc.BADADDR, symbol)

	if not symbol or not address:
		return False

	new_name = idaapi.askstr(0, GetType(address).replace('(', ' ' + symbol + '('), 'Retype Function')
	if not new_name or len(new_name) == 0:
		print('Bad name')
		print(new_name)
		return False

	SetType(address, new_name)

	return True

def lookup_function(func):
	func = idaapi.get_func(func)
	end = 0
	for f in Functions(func.startEA + func.size(), func.startEA + func.size() + 4096):
		end = f
		break

	assert end != 0 and end - func.startEA > 0
	print(end - func.startEA)
	return (func.startEA, end - func.startEA)


class DerailerView(idaapi.simplecustviewer_t):
	def OnDblClick(self, shift):
		res = goto(shift)
		assert res != -1
		self.populate(res)

	def OnKeydown(self, vkey, shift):
		key = chr(vkey)

		if key == 'N':
			rename()
		elif key == 'Y':
			retype()

		self.repopulate()

		return True

	def repopulate(self):
		self.populate(self.last_addr)

	def populate(self, address):
		info = lookup_function(address)
		address = info[0]
		size = info[1]

		self.last_addr = address

		idaapi.msg("Addr: %x, size: %d\n" % (address, size))
		data = idaapi.get_many_bytes(address, size)
		decompiled = decompile(data, address)

		self.ClearLines()
		for line in decompiled:
			self.AddLine(line)

		self.Show()


def decompile_window(address):
	v = DerailerView()

	if not v.Create("Derailer: 0x%x" % address):
		print("Failed to create window!")
		return

	v.populate(address)

def current_function():
	func = idaapi.get_func(ScreenEA())
	return func.startEA

# Adapted from https://github.com/Cisco-Talos/GhIDA/blob/d153e0dddf437b96dbbcd9be23774a538f614317/ghida.py#L608

class DecompileHandler(idaapi.action_handler_t):
	def __init__(self):
		idaapi.action_handler_t.__init__(self)

	def activate(self, ctx):
		decompile_window(current_function())

	def update(self, ctx):
		return idaapi.AST_ENABLE_ALWAYS

class DecompileDebugHandler(idaapi.action_handler_t):
	def __init__(self):
		idaapi.action_handler_t.__init__(self)

	def activate(self, ctx):
		Diagnostics.Debugger.Launch()
		Diagnostics.Debugger.Break()
		decompile_window(current_function())

	def update(self, ctx):
		return idaapi.AST_ENABLE_ALWAYS

class DisHooks(idaapi.UI_Hooks):
	def finish_populating_tform_popup(self, form, popup):
		if idaapi.get_tform_type(form) == idaapi.BWN_DISASMS:
			idaapi.attach_action_to_popup(form, popup, "derailer:decompile", None)
			idaapi.attach_action_to_popup(form, popup, "derailer:decompile_debug", None)

hooks = DisHooks()
hooks.hook()

idaapi.register_action(idaapi.action_desc_t(
	"derailer:decompile",
	"Decompile function with Derailer",
	DecompileHandler(),
	'Ctrl+Shift+M',
	None,
	69
))

idaapi.register_action(idaapi.action_desc_t(
	"derailer:decompile_debug",
	"Decompile function with Derailer (Debug)",
	DecompileDebugHandler(),
	'Ctrl+Shift+N',
	None,
	69
))
