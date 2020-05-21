using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone.Arm;

namespace LibDerailer.CodeGraph
{
    public static class ArmUtil
    {
        public static int GetRegisterNumber(ArmRegisterId reg)
        {
            switch (reg)
            {
                case ArmRegisterId.ARM_REG_R0:   return 0;
                case ArmRegisterId.ARM_REG_R1:   return 1;
                case ArmRegisterId.ARM_REG_R2:   return 2;
                case ArmRegisterId.ARM_REG_R3:   return 3;
                case ArmRegisterId.ARM_REG_R4:   return 4;
                case ArmRegisterId.ARM_REG_R5:   return 5;
                case ArmRegisterId.ARM_REG_R6:   return 6;
                case ArmRegisterId.ARM_REG_R7:   return 7;
                case ArmRegisterId.ARM_REG_R8:   return 8;
                case ArmRegisterId.ARM_REG_R9:   return 9;
                case ArmRegisterId.ARM_REG_R10:  return 10;
                case ArmRegisterId.ARM_REG_R11:  return 11;
                case ArmRegisterId.ARM_REG_R12:  return 12;
                case ArmRegisterId.ARM_REG_R13:  return 13;
                case ArmRegisterId.ARM_REG_R14:  return 14;
                case ArmRegisterId.ARM_REG_R15:  return 15;
                case ArmRegisterId.ARM_REG_CPSR: return 16;
                default:
                    throw new Exception("Unexpected register id");
            }
        }

        public static ArmConditionCode GetOppositeCondition(ArmConditionCode cc)
        {
            switch (cc)
            {
                case ArmConditionCode.Invalid:   return ArmConditionCode.Invalid;
                case ArmConditionCode.ARM_CC_EQ: return ArmConditionCode.ARM_CC_NE;
                case ArmConditionCode.ARM_CC_NE: return ArmConditionCode.ARM_CC_EQ;
                case ArmConditionCode.ARM_CC_HS: return ArmConditionCode.ARM_CC_LO;
                case ArmConditionCode.ARM_CC_LO: return ArmConditionCode.ARM_CC_HS;
                case ArmConditionCode.ARM_CC_MI: return ArmConditionCode.ARM_CC_PL;
                case ArmConditionCode.ARM_CC_PL: return ArmConditionCode.ARM_CC_MI;
                case ArmConditionCode.ARM_CC_VS: return ArmConditionCode.ARM_CC_VC;
                case ArmConditionCode.ARM_CC_VC: return ArmConditionCode.ARM_CC_VS;
                case ArmConditionCode.ARM_CC_HI: return ArmConditionCode.ARM_CC_LS;
                case ArmConditionCode.ARM_CC_LS: return ArmConditionCode.ARM_CC_HI;
                case ArmConditionCode.ARM_CC_GE: return ArmConditionCode.ARM_CC_LT;
                case ArmConditionCode.ARM_CC_LT: return ArmConditionCode.ARM_CC_GE;
                case ArmConditionCode.ARM_CC_GT: return ArmConditionCode.ARM_CC_LE;
                case ArmConditionCode.ARM_CC_LE: return ArmConditionCode.ARM_CC_GT;
                case ArmConditionCode.ARM_CC_AL: return ArmConditionCode.ARM_CC_NE;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cc), cc, null);
            }
        }

        public static bool IsJump(ArmInstruction instruction)
        {
            return !instruction.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_CALL) &&
                   (instruction.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_BRANCH_RELATIVE) ||
                    instruction.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_JUMP) ||
                    instruction.Details.IsRegisterExplicitlyWritten(ArmRegisterId.ARM_REG_PC));
        }
    }
}