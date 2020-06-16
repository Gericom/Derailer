# Interop between IDA and Derailer
# https://github.com/Gericom/Derailer

import os

# ...\Derailer\Derailer\bin\Debug\
assert "DERAILER_ROOT" in os.environ
DERAILER_PATH = os.getenv("DERAILER_ROOT")

def decompile(data, addr):
	bin_path = DERAILER_PATH + "in.bin"
	out_path = DERAILER_PATH + "out.c"

	with open(bin_path, 'wb') as file:
		file.write(data)

	try:
		os.remove(out_path)
	except:
		pass

	os.system(os.path.join(DERAILER_PATH, "Derailer.exe") + " Analyze-File " + bin_path + " " + out_path + " %u" % addr)

	try:
		with open(out_path, 'r') as file:
			return file.readlines()
	except:
		return ["Derailer failed to decompile the function"]

def decompile_window(address, size):
	v = idaapi.simplecustviewer_t()

	if not v.Create("Derailer: 0x%x" % address):
		print("Failed to create window!")
		return

	idaapi.msg("Addr: %x, size: %d\n" % (address, size))
	data = idaapi.get_many_bytes(address, size)
	decompiled = decompile(data, address)

	for line in decompiled:
		v.AddLine(line)

	v.Show()


def current_function():
	func = idaapi.get_func(ScreenEA())
	end = 0
	for f in Functions(func.startEA + func.size(), func.startEA + func.size() + 4096):
		end = f
		break

	assert end != 0 and end - func.startEA > 0
	return (func.startEA, end - func.startEA)

# Adapted from https://github.com/Cisco-Talos/GhIDA/blob/d153e0dddf437b96dbbcd9be23774a538f614317/ghida.py#L608

class DecompileHandler(idaapi.action_handler_t):
	def __init__(self):
		idaapi.action_handler_t.__init__(self)

	def activate(self, ctx):
		func = current_function()
		decompile_window(func[0], func[1])

	def update(self, ctx):
		return idaapi.AST_ENABLE_ALWAYS

class DisHooks(idaapi.UI_Hooks):
	def finish_populating_tform_popup(self, form, popup):
		if idaapi.get_tform_type(form) == idaapi.BWN_DISASMS:
			idaapi.attach_action_to_popup(form, popup, "derailer:decompile", None)

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
