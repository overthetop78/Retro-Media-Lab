namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

public sealed partial class Cpu68000<TBus> where TBus : class, ICpu68000Bus
{
    private void ExecuteInstruction()
    {
        switch (InstructionMap[(int)REG_IR])
        {
            case 0: m68k_op_ori_8_d(); break;
            case 1: m68k_op_illegal(); break;
            case 2: m68k_op_ori_8_ai(); break;
            case 3: m68k_op_ori_8_pi(); break;
            case 4: m68k_op_ori_8_pi7(); break;
            case 5: m68k_op_ori_8_pd(); break;
            case 6: m68k_op_ori_8_pd7(); break;
            case 7: m68k_op_ori_8_di(); break;
            case 8: m68k_op_ori_8_ix(); break;
            case 9: m68k_op_ori_8_aw(); break;
            case 10: m68k_op_ori_8_al(); break;
            case 11: m68k_op_ori_16_toc(); break;
            case 12: m68k_op_ori_16_d(); break;
            case 13: m68k_op_ori_16_ai(); break;
            case 14: m68k_op_ori_16_pi(); break;
            case 15: m68k_op_ori_16_pd(); break;
            case 16: m68k_op_ori_16_di(); break;
            case 17: m68k_op_ori_16_ix(); break;
            case 18: m68k_op_ori_16_aw(); break;
            case 19: m68k_op_ori_16_al(); break;
            case 20: m68k_op_ori_16_tos(); break;
            case 21: m68k_op_ori_32_d(); break;
            case 22: m68k_op_ori_32_ai(); break;
            case 23: m68k_op_ori_32_pi(); break;
            case 24: m68k_op_ori_32_pd(); break;
            case 25: m68k_op_ori_32_di(); break;
            case 26: m68k_op_ori_32_ix(); break;
            case 27: m68k_op_ori_32_aw(); break;
            case 28: m68k_op_ori_32_al(); break;
            case 29: m68k_op_btst_32_r_d(); break;
            case 30: m68k_op_movep_16_er(); break;
            case 31: m68k_op_btst_8_r_ai(); break;
            case 32: m68k_op_btst_8_r_pi(); break;
            case 33: m68k_op_btst_8_r_pi7(); break;
            case 34: m68k_op_btst_8_r_pd(); break;
            case 35: m68k_op_btst_8_r_pd7(); break;
            case 36: m68k_op_btst_8_r_di(); break;
            case 37: m68k_op_btst_8_r_ix(); break;
            case 38: m68k_op_btst_8_r_aw(); break;
            case 39: m68k_op_btst_8_r_al(); break;
            case 40: m68k_op_btst_8_r_pcdi(); break;
            case 41: m68k_op_btst_8_r_pcix(); break;
            case 42: m68k_op_btst_8_r_i(); break;
            case 43: m68k_op_bchg_32_r_d(); break;
            case 44: m68k_op_movep_32_er(); break;
            case 45: m68k_op_bchg_8_r_ai(); break;
            case 46: m68k_op_bchg_8_r_pi(); break;
            case 47: m68k_op_bchg_8_r_pi7(); break;
            case 48: m68k_op_bchg_8_r_pd(); break;
            case 49: m68k_op_bchg_8_r_pd7(); break;
            case 50: m68k_op_bchg_8_r_di(); break;
            case 51: m68k_op_bchg_8_r_ix(); break;
            case 52: m68k_op_bchg_8_r_aw(); break;
            case 53: m68k_op_bchg_8_r_al(); break;
            case 54: m68k_op_bclr_32_r_d(); break;
            case 55: m68k_op_movep_16_re(); break;
            case 56: m68k_op_bclr_8_r_ai(); break;
            case 57: m68k_op_bclr_8_r_pi(); break;
            case 58: m68k_op_bclr_8_r_pi7(); break;
            case 59: m68k_op_bclr_8_r_pd(); break;
            case 60: m68k_op_bclr_8_r_pd7(); break;
            case 61: m68k_op_bclr_8_r_di(); break;
            case 62: m68k_op_bclr_8_r_ix(); break;
            case 63: m68k_op_bclr_8_r_aw(); break;
            case 64: m68k_op_bclr_8_r_al(); break;
            case 65: m68k_op_bset_32_r_d(); break;
            case 66: m68k_op_movep_32_re(); break;
            case 67: m68k_op_bset_8_r_ai(); break;
            case 68: m68k_op_bset_8_r_pi(); break;
            case 69: m68k_op_bset_8_r_pi7(); break;
            case 70: m68k_op_bset_8_r_pd(); break;
            case 71: m68k_op_bset_8_r_pd7(); break;
            case 72: m68k_op_bset_8_r_di(); break;
            case 73: m68k_op_bset_8_r_ix(); break;
            case 74: m68k_op_bset_8_r_aw(); break;
            case 75: m68k_op_bset_8_r_al(); break;
            case 76: m68k_op_andi_8_d(); break;
            case 77: m68k_op_andi_8_ai(); break;
            case 78: m68k_op_andi_8_pi(); break;
            case 79: m68k_op_andi_8_pi7(); break;
            case 80: m68k_op_andi_8_pd(); break;
            case 81: m68k_op_andi_8_pd7(); break;
            case 82: m68k_op_andi_8_di(); break;
            case 83: m68k_op_andi_8_ix(); break;
            case 84: m68k_op_andi_8_aw(); break;
            case 85: m68k_op_andi_8_al(); break;
            case 86: m68k_op_andi_16_toc(); break;
            case 87: m68k_op_andi_16_d(); break;
            case 88: m68k_op_andi_16_ai(); break;
            case 89: m68k_op_andi_16_pi(); break;
            case 90: m68k_op_andi_16_pd(); break;
            case 91: m68k_op_andi_16_di(); break;
            case 92: m68k_op_andi_16_ix(); break;
            case 93: m68k_op_andi_16_aw(); break;
            case 94: m68k_op_andi_16_al(); break;
            case 95: m68k_op_andi_16_tos(); break;
            case 96: m68k_op_andi_32_d(); break;
            case 97: m68k_op_andi_32_ai(); break;
            case 98: m68k_op_andi_32_pi(); break;
            case 99: m68k_op_andi_32_pd(); break;
            case 100: m68k_op_andi_32_di(); break;
            case 101: m68k_op_andi_32_ix(); break;
            case 102: m68k_op_andi_32_aw(); break;
            case 103: m68k_op_andi_32_al(); break;
            case 104: m68k_op_subi_8_d(); break;
            case 105: m68k_op_subi_8_ai(); break;
            case 106: m68k_op_subi_8_pi(); break;
            case 107: m68k_op_subi_8_pi7(); break;
            case 108: m68k_op_subi_8_pd(); break;
            case 109: m68k_op_subi_8_pd7(); break;
            case 110: m68k_op_subi_8_di(); break;
            case 111: m68k_op_subi_8_ix(); break;
            case 112: m68k_op_subi_8_aw(); break;
            case 113: m68k_op_subi_8_al(); break;
            case 114: m68k_op_subi_16_d(); break;
            case 115: m68k_op_subi_16_ai(); break;
            case 116: m68k_op_subi_16_pi(); break;
            case 117: m68k_op_subi_16_pd(); break;
            case 118: m68k_op_subi_16_di(); break;
            case 119: m68k_op_subi_16_ix(); break;
            case 120: m68k_op_subi_16_aw(); break;
            case 121: m68k_op_subi_16_al(); break;
            case 122: m68k_op_subi_32_d(); break;
            case 123: m68k_op_subi_32_ai(); break;
            case 124: m68k_op_subi_32_pi(); break;
            case 125: m68k_op_subi_32_pd(); break;
            case 126: m68k_op_subi_32_di(); break;
            case 127: m68k_op_subi_32_ix(); break;
            case 128: m68k_op_subi_32_aw(); break;
            case 129: m68k_op_subi_32_al(); break;
            case 130: m68k_op_addi_8_d(); break;
            case 131: m68k_op_addi_8_ai(); break;
            case 132: m68k_op_addi_8_pi(); break;
            case 133: m68k_op_addi_8_pi7(); break;
            case 134: m68k_op_addi_8_pd(); break;
            case 135: m68k_op_addi_8_pd7(); break;
            case 136: m68k_op_addi_8_di(); break;
            case 137: m68k_op_addi_8_ix(); break;
            case 138: m68k_op_addi_8_aw(); break;
            case 139: m68k_op_addi_8_al(); break;
            case 140: m68k_op_addi_16_d(); break;
            case 141: m68k_op_addi_16_ai(); break;
            case 142: m68k_op_addi_16_pi(); break;
            case 143: m68k_op_addi_16_pd(); break;
            case 144: m68k_op_addi_16_di(); break;
            case 145: m68k_op_addi_16_ix(); break;
            case 146: m68k_op_addi_16_aw(); break;
            case 147: m68k_op_addi_16_al(); break;
            case 148: m68k_op_addi_32_d(); break;
            case 149: m68k_op_addi_32_ai(); break;
            case 150: m68k_op_addi_32_pi(); break;
            case 151: m68k_op_addi_32_pd(); break;
            case 152: m68k_op_addi_32_di(); break;
            case 153: m68k_op_addi_32_ix(); break;
            case 154: m68k_op_addi_32_aw(); break;
            case 155: m68k_op_addi_32_al(); break;
            case 156: m68k_op_btst_32_s_d(); break;
            case 157: m68k_op_btst_8_s_ai(); break;
            case 158: m68k_op_btst_8_s_pi(); break;
            case 159: m68k_op_btst_8_s_pi7(); break;
            case 160: m68k_op_btst_8_s_pd(); break;
            case 161: m68k_op_btst_8_s_pd7(); break;
            case 162: m68k_op_btst_8_s_di(); break;
            case 163: m68k_op_btst_8_s_ix(); break;
            case 164: m68k_op_btst_8_s_aw(); break;
            case 165: m68k_op_btst_8_s_al(); break;
            case 166: m68k_op_btst_8_s_pcdi(); break;
            case 167: m68k_op_btst_8_s_pcix(); break;
            case 168: m68k_op_bchg_32_s_d(); break;
            case 169: m68k_op_bchg_8_s_ai(); break;
            case 170: m68k_op_bchg_8_s_pi(); break;
            case 171: m68k_op_bchg_8_s_pi7(); break;
            case 172: m68k_op_bchg_8_s_pd(); break;
            case 173: m68k_op_bchg_8_s_pd7(); break;
            case 174: m68k_op_bchg_8_s_di(); break;
            case 175: m68k_op_bchg_8_s_ix(); break;
            case 176: m68k_op_bchg_8_s_aw(); break;
            case 177: m68k_op_bchg_8_s_al(); break;
            case 178: m68k_op_bclr_32_s_d(); break;
            case 179: m68k_op_bclr_8_s_ai(); break;
            case 180: m68k_op_bclr_8_s_pi(); break;
            case 181: m68k_op_bclr_8_s_pi7(); break;
            case 182: m68k_op_bclr_8_s_pd(); break;
            case 183: m68k_op_bclr_8_s_pd7(); break;
            case 184: m68k_op_bclr_8_s_di(); break;
            case 185: m68k_op_bclr_8_s_ix(); break;
            case 186: m68k_op_bclr_8_s_aw(); break;
            case 187: m68k_op_bclr_8_s_al(); break;
            case 188: m68k_op_bset_32_s_d(); break;
            case 189: m68k_op_bset_8_s_ai(); break;
            case 190: m68k_op_bset_8_s_pi(); break;
            case 191: m68k_op_bset_8_s_pi7(); break;
            case 192: m68k_op_bset_8_s_pd(); break;
            case 193: m68k_op_bset_8_s_pd7(); break;
            case 194: m68k_op_bset_8_s_di(); break;
            case 195: m68k_op_bset_8_s_ix(); break;
            case 196: m68k_op_bset_8_s_aw(); break;
            case 197: m68k_op_bset_8_s_al(); break;
            case 198: m68k_op_eori_8_d(); break;
            case 199: m68k_op_eori_8_ai(); break;
            case 200: m68k_op_eori_8_pi(); break;
            case 201: m68k_op_eori_8_pi7(); break;
            case 202: m68k_op_eori_8_pd(); break;
            case 203: m68k_op_eori_8_pd7(); break;
            case 204: m68k_op_eori_8_di(); break;
            case 205: m68k_op_eori_8_ix(); break;
            case 206: m68k_op_eori_8_aw(); break;
            case 207: m68k_op_eori_8_al(); break;
            case 208: m68k_op_eori_16_toc(); break;
            case 209: m68k_op_eori_16_d(); break;
            case 210: m68k_op_eori_16_ai(); break;
            case 211: m68k_op_eori_16_pi(); break;
            case 212: m68k_op_eori_16_pd(); break;
            case 213: m68k_op_eori_16_di(); break;
            case 214: m68k_op_eori_16_ix(); break;
            case 215: m68k_op_eori_16_aw(); break;
            case 216: m68k_op_eori_16_al(); break;
            case 217: m68k_op_eori_16_tos(); break;
            case 218: m68k_op_eori_32_d(); break;
            case 219: m68k_op_eori_32_ai(); break;
            case 220: m68k_op_eori_32_pi(); break;
            case 221: m68k_op_eori_32_pd(); break;
            case 222: m68k_op_eori_32_di(); break;
            case 223: m68k_op_eori_32_ix(); break;
            case 224: m68k_op_eori_32_aw(); break;
            case 225: m68k_op_eori_32_al(); break;
            case 226: m68k_op_cmpi_8_d(); break;
            case 227: m68k_op_cmpi_8_ai(); break;
            case 228: m68k_op_cmpi_8_pi(); break;
            case 229: m68k_op_cmpi_8_pi7(); break;
            case 230: m68k_op_cmpi_8_pd(); break;
            case 231: m68k_op_cmpi_8_pd7(); break;
            case 232: m68k_op_cmpi_8_di(); break;
            case 233: m68k_op_cmpi_8_ix(); break;
            case 234: m68k_op_cmpi_8_aw(); break;
            case 235: m68k_op_cmpi_8_al(); break;
            case 236: m68k_op_cmpi_16_d(); break;
            case 237: m68k_op_cmpi_16_ai(); break;
            case 238: m68k_op_cmpi_16_pi(); break;
            case 239: m68k_op_cmpi_16_pd(); break;
            case 240: m68k_op_cmpi_16_di(); break;
            case 241: m68k_op_cmpi_16_ix(); break;
            case 242: m68k_op_cmpi_16_aw(); break;
            case 243: m68k_op_cmpi_16_al(); break;
            case 244: m68k_op_cmpi_32_d(); break;
            case 245: m68k_op_cmpi_32_ai(); break;
            case 246: m68k_op_cmpi_32_pi(); break;
            case 247: m68k_op_cmpi_32_pd(); break;
            case 248: m68k_op_cmpi_32_di(); break;
            case 249: m68k_op_cmpi_32_ix(); break;
            case 250: m68k_op_cmpi_32_aw(); break;
            case 251: m68k_op_cmpi_32_al(); break;
            case 252: m68k_op_move_8_d_d(); break;
            case 253: m68k_op_move_8_d_ai(); break;
            case 254: m68k_op_move_8_d_pi(); break;
            case 255: m68k_op_move_8_d_pi7(); break;
            case 256: m68k_op_move_8_d_pd(); break;
            case 257: m68k_op_move_8_d_pd7(); break;
            case 258: m68k_op_move_8_d_di(); break;
            case 259: m68k_op_move_8_d_ix(); break;
            case 260: m68k_op_move_8_d_aw(); break;
            case 261: m68k_op_move_8_d_al(); break;
            case 262: m68k_op_move_8_d_pcdi(); break;
            case 263: m68k_op_move_8_d_pcix(); break;
            case 264: m68k_op_move_8_d_i(); break;
            case 265: m68k_op_move_8_ai_d(); break;
            case 266: m68k_op_move_8_ai_ai(); break;
            case 267: m68k_op_move_8_ai_pi(); break;
            case 268: m68k_op_move_8_ai_pi7(); break;
            case 269: m68k_op_move_8_ai_pd(); break;
            case 270: m68k_op_move_8_ai_pd7(); break;
            case 271: m68k_op_move_8_ai_di(); break;
            case 272: m68k_op_move_8_ai_ix(); break;
            case 273: m68k_op_move_8_ai_aw(); break;
            case 274: m68k_op_move_8_ai_al(); break;
            case 275: m68k_op_move_8_ai_pcdi(); break;
            case 276: m68k_op_move_8_ai_pcix(); break;
            case 277: m68k_op_move_8_ai_i(); break;
            case 278: m68k_op_move_8_pi_d(); break;
            case 279: m68k_op_move_8_pi_ai(); break;
            case 280: m68k_op_move_8_pi_pi(); break;
            case 281: m68k_op_move_8_pi_pi7(); break;
            case 282: m68k_op_move_8_pi_pd(); break;
            case 283: m68k_op_move_8_pi_pd7(); break;
            case 284: m68k_op_move_8_pi_di(); break;
            case 285: m68k_op_move_8_pi_ix(); break;
            case 286: m68k_op_move_8_pi_aw(); break;
            case 287: m68k_op_move_8_pi_al(); break;
            case 288: m68k_op_move_8_pi_pcdi(); break;
            case 289: m68k_op_move_8_pi_pcix(); break;
            case 290: m68k_op_move_8_pi_i(); break;
            case 291: m68k_op_move_8_pd_d(); break;
            case 292: m68k_op_move_8_pd_ai(); break;
            case 293: m68k_op_move_8_pd_pi(); break;
            case 294: m68k_op_move_8_pd_pi7(); break;
            case 295: m68k_op_move_8_pd_pd(); break;
            case 296: m68k_op_move_8_pd_pd7(); break;
            case 297: m68k_op_move_8_pd_di(); break;
            case 298: m68k_op_move_8_pd_ix(); break;
            case 299: m68k_op_move_8_pd_aw(); break;
            case 300: m68k_op_move_8_pd_al(); break;
            case 301: m68k_op_move_8_pd_pcdi(); break;
            case 302: m68k_op_move_8_pd_pcix(); break;
            case 303: m68k_op_move_8_pd_i(); break;
            case 304: m68k_op_move_8_di_d(); break;
            case 305: m68k_op_move_8_di_ai(); break;
            case 306: m68k_op_move_8_di_pi(); break;
            case 307: m68k_op_move_8_di_pi7(); break;
            case 308: m68k_op_move_8_di_pd(); break;
            case 309: m68k_op_move_8_di_pd7(); break;
            case 310: m68k_op_move_8_di_di(); break;
            case 311: m68k_op_move_8_di_ix(); break;
            case 312: m68k_op_move_8_di_aw(); break;
            case 313: m68k_op_move_8_di_al(); break;
            case 314: m68k_op_move_8_di_pcdi(); break;
            case 315: m68k_op_move_8_di_pcix(); break;
            case 316: m68k_op_move_8_di_i(); break;
            case 317: m68k_op_move_8_ix_d(); break;
            case 318: m68k_op_move_8_ix_ai(); break;
            case 319: m68k_op_move_8_ix_pi(); break;
            case 320: m68k_op_move_8_ix_pi7(); break;
            case 321: m68k_op_move_8_ix_pd(); break;
            case 322: m68k_op_move_8_ix_pd7(); break;
            case 323: m68k_op_move_8_ix_di(); break;
            case 324: m68k_op_move_8_ix_ix(); break;
            case 325: m68k_op_move_8_ix_aw(); break;
            case 326: m68k_op_move_8_ix_al(); break;
            case 327: m68k_op_move_8_ix_pcdi(); break;
            case 328: m68k_op_move_8_ix_pcix(); break;
            case 329: m68k_op_move_8_ix_i(); break;
            case 330: m68k_op_move_8_aw_d(); break;
            case 331: m68k_op_move_8_aw_ai(); break;
            case 332: m68k_op_move_8_aw_pi(); break;
            case 333: m68k_op_move_8_aw_pi7(); break;
            case 334: m68k_op_move_8_aw_pd(); break;
            case 335: m68k_op_move_8_aw_pd7(); break;
            case 336: m68k_op_move_8_aw_di(); break;
            case 337: m68k_op_move_8_aw_ix(); break;
            case 338: m68k_op_move_8_aw_aw(); break;
            case 339: m68k_op_move_8_aw_al(); break;
            case 340: m68k_op_move_8_aw_pcdi(); break;
            case 341: m68k_op_move_8_aw_pcix(); break;
            case 342: m68k_op_move_8_aw_i(); break;
            case 343: m68k_op_move_8_al_d(); break;
            case 344: m68k_op_move_8_al_ai(); break;
            case 345: m68k_op_move_8_al_pi(); break;
            case 346: m68k_op_move_8_al_pi7(); break;
            case 347: m68k_op_move_8_al_pd(); break;
            case 348: m68k_op_move_8_al_pd7(); break;
            case 349: m68k_op_move_8_al_di(); break;
            case 350: m68k_op_move_8_al_ix(); break;
            case 351: m68k_op_move_8_al_aw(); break;
            case 352: m68k_op_move_8_al_al(); break;
            case 353: m68k_op_move_8_al_pcdi(); break;
            case 354: m68k_op_move_8_al_pcix(); break;
            case 355: m68k_op_move_8_al_i(); break;
            case 356: m68k_op_move_8_pi7_d(); break;
            case 357: m68k_op_move_8_pi7_ai(); break;
            case 358: m68k_op_move_8_pi7_pi(); break;
            case 359: m68k_op_move_8_pi7_pi7(); break;
            case 360: m68k_op_move_8_pi7_pd(); break;
            case 361: m68k_op_move_8_pi7_pd7(); break;
            case 362: m68k_op_move_8_pi7_di(); break;
            case 363: m68k_op_move_8_pi7_ix(); break;
            case 364: m68k_op_move_8_pi7_aw(); break;
            case 365: m68k_op_move_8_pi7_al(); break;
            case 366: m68k_op_move_8_pi7_pcdi(); break;
            case 367: m68k_op_move_8_pi7_pcix(); break;
            case 368: m68k_op_move_8_pi7_i(); break;
            case 369: m68k_op_move_8_pd7_d(); break;
            case 370: m68k_op_move_8_pd7_ai(); break;
            case 371: m68k_op_move_8_pd7_pi(); break;
            case 372: m68k_op_move_8_pd7_pi7(); break;
            case 373: m68k_op_move_8_pd7_pd(); break;
            case 374: m68k_op_move_8_pd7_pd7(); break;
            case 375: m68k_op_move_8_pd7_di(); break;
            case 376: m68k_op_move_8_pd7_ix(); break;
            case 377: m68k_op_move_8_pd7_aw(); break;
            case 378: m68k_op_move_8_pd7_al(); break;
            case 379: m68k_op_move_8_pd7_pcdi(); break;
            case 380: m68k_op_move_8_pd7_pcix(); break;
            case 381: m68k_op_move_8_pd7_i(); break;
            case 382: m68k_op_move_32_d_d(); break;
            case 383: m68k_op_move_32_d_a(); break;
            case 384: m68k_op_move_32_d_ai(); break;
            case 385: m68k_op_move_32_d_pi(); break;
            case 386: m68k_op_move_32_d_pd(); break;
            case 387: m68k_op_move_32_d_di(); break;
            case 388: m68k_op_move_32_d_ix(); break;
            case 389: m68k_op_move_32_d_aw(); break;
            case 390: m68k_op_move_32_d_al(); break;
            case 391: m68k_op_move_32_d_pcdi(); break;
            case 392: m68k_op_move_32_d_pcix(); break;
            case 393: m68k_op_move_32_d_i(); break;
            case 394: m68k_op_movea_32_d(); break;
            case 395: m68k_op_movea_32_a(); break;
            case 396: m68k_op_movea_32_ai(); break;
            case 397: m68k_op_movea_32_pi(); break;
            case 398: m68k_op_movea_32_pd(); break;
            case 399: m68k_op_movea_32_di(); break;
            case 400: m68k_op_movea_32_ix(); break;
            case 401: m68k_op_movea_32_aw(); break;
            case 402: m68k_op_movea_32_al(); break;
            case 403: m68k_op_movea_32_pcdi(); break;
            case 404: m68k_op_movea_32_pcix(); break;
            case 405: m68k_op_movea_32_i(); break;
            case 406: m68k_op_move_32_ai_d(); break;
            case 407: m68k_op_move_32_ai_a(); break;
            case 408: m68k_op_move_32_ai_ai(); break;
            case 409: m68k_op_move_32_ai_pi(); break;
            case 410: m68k_op_move_32_ai_pd(); break;
            case 411: m68k_op_move_32_ai_di(); break;
            case 412: m68k_op_move_32_ai_ix(); break;
            case 413: m68k_op_move_32_ai_aw(); break;
            case 414: m68k_op_move_32_ai_al(); break;
            case 415: m68k_op_move_32_ai_pcdi(); break;
            case 416: m68k_op_move_32_ai_pcix(); break;
            case 417: m68k_op_move_32_ai_i(); break;
            case 418: m68k_op_move_32_pi_d(); break;
            case 419: m68k_op_move_32_pi_a(); break;
            case 420: m68k_op_move_32_pi_ai(); break;
            case 421: m68k_op_move_32_pi_pi(); break;
            case 422: m68k_op_move_32_pi_pd(); break;
            case 423: m68k_op_move_32_pi_di(); break;
            case 424: m68k_op_move_32_pi_ix(); break;
            case 425: m68k_op_move_32_pi_aw(); break;
            case 426: m68k_op_move_32_pi_al(); break;
            case 427: m68k_op_move_32_pi_pcdi(); break;
            case 428: m68k_op_move_32_pi_pcix(); break;
            case 429: m68k_op_move_32_pi_i(); break;
            case 430: m68k_op_move_32_pd_d(); break;
            case 431: m68k_op_move_32_pd_a(); break;
            case 432: m68k_op_move_32_pd_ai(); break;
            case 433: m68k_op_move_32_pd_pi(); break;
            case 434: m68k_op_move_32_pd_pd(); break;
            case 435: m68k_op_move_32_pd_di(); break;
            case 436: m68k_op_move_32_pd_ix(); break;
            case 437: m68k_op_move_32_pd_aw(); break;
            case 438: m68k_op_move_32_pd_al(); break;
            case 439: m68k_op_move_32_pd_pcdi(); break;
            case 440: m68k_op_move_32_pd_pcix(); break;
            case 441: m68k_op_move_32_pd_i(); break;
            case 442: m68k_op_move_32_di_d(); break;
            case 443: m68k_op_move_32_di_a(); break;
            case 444: m68k_op_move_32_di_ai(); break;
            case 445: m68k_op_move_32_di_pi(); break;
            case 446: m68k_op_move_32_di_pd(); break;
            case 447: m68k_op_move_32_di_di(); break;
            case 448: m68k_op_move_32_di_ix(); break;
            case 449: m68k_op_move_32_di_aw(); break;
            case 450: m68k_op_move_32_di_al(); break;
            case 451: m68k_op_move_32_di_pcdi(); break;
            case 452: m68k_op_move_32_di_pcix(); break;
            case 453: m68k_op_move_32_di_i(); break;
            case 454: m68k_op_move_32_ix_d(); break;
            case 455: m68k_op_move_32_ix_a(); break;
            case 456: m68k_op_move_32_ix_ai(); break;
            case 457: m68k_op_move_32_ix_pi(); break;
            case 458: m68k_op_move_32_ix_pd(); break;
            case 459: m68k_op_move_32_ix_di(); break;
            case 460: m68k_op_move_32_ix_ix(); break;
            case 461: m68k_op_move_32_ix_aw(); break;
            case 462: m68k_op_move_32_ix_al(); break;
            case 463: m68k_op_move_32_ix_pcdi(); break;
            case 464: m68k_op_move_32_ix_pcix(); break;
            case 465: m68k_op_move_32_ix_i(); break;
            case 466: m68k_op_move_32_aw_d(); break;
            case 467: m68k_op_move_32_aw_a(); break;
            case 468: m68k_op_move_32_aw_ai(); break;
            case 469: m68k_op_move_32_aw_pi(); break;
            case 470: m68k_op_move_32_aw_pd(); break;
            case 471: m68k_op_move_32_aw_di(); break;
            case 472: m68k_op_move_32_aw_ix(); break;
            case 473: m68k_op_move_32_aw_aw(); break;
            case 474: m68k_op_move_32_aw_al(); break;
            case 475: m68k_op_move_32_aw_pcdi(); break;
            case 476: m68k_op_move_32_aw_pcix(); break;
            case 477: m68k_op_move_32_aw_i(); break;
            case 478: m68k_op_move_32_al_d(); break;
            case 479: m68k_op_move_32_al_a(); break;
            case 480: m68k_op_move_32_al_ai(); break;
            case 481: m68k_op_move_32_al_pi(); break;
            case 482: m68k_op_move_32_al_pd(); break;
            case 483: m68k_op_move_32_al_di(); break;
            case 484: m68k_op_move_32_al_ix(); break;
            case 485: m68k_op_move_32_al_aw(); break;
            case 486: m68k_op_move_32_al_al(); break;
            case 487: m68k_op_move_32_al_pcdi(); break;
            case 488: m68k_op_move_32_al_pcix(); break;
            case 489: m68k_op_move_32_al_i(); break;
            case 490: m68k_op_move_16_d_d(); break;
            case 491: m68k_op_move_16_d_a(); break;
            case 492: m68k_op_move_16_d_ai(); break;
            case 493: m68k_op_move_16_d_pi(); break;
            case 494: m68k_op_move_16_d_pd(); break;
            case 495: m68k_op_move_16_d_di(); break;
            case 496: m68k_op_move_16_d_ix(); break;
            case 497: m68k_op_move_16_d_aw(); break;
            case 498: m68k_op_move_16_d_al(); break;
            case 499: m68k_op_move_16_d_pcdi(); break;
            case 500: m68k_op_move_16_d_pcix(); break;
            case 501: m68k_op_move_16_d_i(); break;
            case 502: m68k_op_movea_16_d(); break;
            case 503: m68k_op_movea_16_a(); break;
            case 504: m68k_op_movea_16_ai(); break;
            case 505: m68k_op_movea_16_pi(); break;
            case 506: m68k_op_movea_16_pd(); break;
            case 507: m68k_op_movea_16_di(); break;
            case 508: m68k_op_movea_16_ix(); break;
            case 509: m68k_op_movea_16_aw(); break;
            case 510: m68k_op_movea_16_al(); break;
            case 511: m68k_op_movea_16_pcdi(); break;
            case 512: m68k_op_movea_16_pcix(); break;
            case 513: m68k_op_movea_16_i(); break;
            case 514: m68k_op_move_16_ai_d(); break;
            case 515: m68k_op_move_16_ai_a(); break;
            case 516: m68k_op_move_16_ai_ai(); break;
            case 517: m68k_op_move_16_ai_pi(); break;
            case 518: m68k_op_move_16_ai_pd(); break;
            case 519: m68k_op_move_16_ai_di(); break;
            case 520: m68k_op_move_16_ai_ix(); break;
            case 521: m68k_op_move_16_ai_aw(); break;
            case 522: m68k_op_move_16_ai_al(); break;
            case 523: m68k_op_move_16_ai_pcdi(); break;
            case 524: m68k_op_move_16_ai_pcix(); break;
            case 525: m68k_op_move_16_ai_i(); break;
            case 526: m68k_op_move_16_pi_d(); break;
            case 527: m68k_op_move_16_pi_a(); break;
            case 528: m68k_op_move_16_pi_ai(); break;
            case 529: m68k_op_move_16_pi_pi(); break;
            case 530: m68k_op_move_16_pi_pd(); break;
            case 531: m68k_op_move_16_pi_di(); break;
            case 532: m68k_op_move_16_pi_ix(); break;
            case 533: m68k_op_move_16_pi_aw(); break;
            case 534: m68k_op_move_16_pi_al(); break;
            case 535: m68k_op_move_16_pi_pcdi(); break;
            case 536: m68k_op_move_16_pi_pcix(); break;
            case 537: m68k_op_move_16_pi_i(); break;
            case 538: m68k_op_move_16_pd_d(); break;
            case 539: m68k_op_move_16_pd_a(); break;
            case 540: m68k_op_move_16_pd_ai(); break;
            case 541: m68k_op_move_16_pd_pi(); break;
            case 542: m68k_op_move_16_pd_pd(); break;
            case 543: m68k_op_move_16_pd_di(); break;
            case 544: m68k_op_move_16_pd_ix(); break;
            case 545: m68k_op_move_16_pd_aw(); break;
            case 546: m68k_op_move_16_pd_al(); break;
            case 547: m68k_op_move_16_pd_pcdi(); break;
            case 548: m68k_op_move_16_pd_pcix(); break;
            case 549: m68k_op_move_16_pd_i(); break;
            case 550: m68k_op_move_16_di_d(); break;
            case 551: m68k_op_move_16_di_a(); break;
            case 552: m68k_op_move_16_di_ai(); break;
            case 553: m68k_op_move_16_di_pi(); break;
            case 554: m68k_op_move_16_di_pd(); break;
            case 555: m68k_op_move_16_di_di(); break;
            case 556: m68k_op_move_16_di_ix(); break;
            case 557: m68k_op_move_16_di_aw(); break;
            case 558: m68k_op_move_16_di_al(); break;
            case 559: m68k_op_move_16_di_pcdi(); break;
            case 560: m68k_op_move_16_di_pcix(); break;
            case 561: m68k_op_move_16_di_i(); break;
            case 562: m68k_op_move_16_ix_d(); break;
            case 563: m68k_op_move_16_ix_a(); break;
            case 564: m68k_op_move_16_ix_ai(); break;
            case 565: m68k_op_move_16_ix_pi(); break;
            case 566: m68k_op_move_16_ix_pd(); break;
            case 567: m68k_op_move_16_ix_di(); break;
            case 568: m68k_op_move_16_ix_ix(); break;
            case 569: m68k_op_move_16_ix_aw(); break;
            case 570: m68k_op_move_16_ix_al(); break;
            case 571: m68k_op_move_16_ix_pcdi(); break;
            case 572: m68k_op_move_16_ix_pcix(); break;
            case 573: m68k_op_move_16_ix_i(); break;
            case 574: m68k_op_move_16_aw_d(); break;
            case 575: m68k_op_move_16_aw_a(); break;
            case 576: m68k_op_move_16_aw_ai(); break;
            case 577: m68k_op_move_16_aw_pi(); break;
            case 578: m68k_op_move_16_aw_pd(); break;
            case 579: m68k_op_move_16_aw_di(); break;
            case 580: m68k_op_move_16_aw_ix(); break;
            case 581: m68k_op_move_16_aw_aw(); break;
            case 582: m68k_op_move_16_aw_al(); break;
            case 583: m68k_op_move_16_aw_pcdi(); break;
            case 584: m68k_op_move_16_aw_pcix(); break;
            case 585: m68k_op_move_16_aw_i(); break;
            case 586: m68k_op_move_16_al_d(); break;
            case 587: m68k_op_move_16_al_a(); break;
            case 588: m68k_op_move_16_al_ai(); break;
            case 589: m68k_op_move_16_al_pi(); break;
            case 590: m68k_op_move_16_al_pd(); break;
            case 591: m68k_op_move_16_al_di(); break;
            case 592: m68k_op_move_16_al_ix(); break;
            case 593: m68k_op_move_16_al_aw(); break;
            case 594: m68k_op_move_16_al_al(); break;
            case 595: m68k_op_move_16_al_pcdi(); break;
            case 596: m68k_op_move_16_al_pcix(); break;
            case 597: m68k_op_move_16_al_i(); break;
            case 598: m68k_op_negx_8_d(); break;
            case 599: m68k_op_negx_8_ai(); break;
            case 600: m68k_op_negx_8_pi(); break;
            case 601: m68k_op_negx_8_pi7(); break;
            case 602: m68k_op_negx_8_pd(); break;
            case 603: m68k_op_negx_8_pd7(); break;
            case 604: m68k_op_negx_8_di(); break;
            case 605: m68k_op_negx_8_ix(); break;
            case 606: m68k_op_negx_8_aw(); break;
            case 607: m68k_op_negx_8_al(); break;
            case 608: m68k_op_negx_16_d(); break;
            case 609: m68k_op_negx_16_ai(); break;
            case 610: m68k_op_negx_16_pi(); break;
            case 611: m68k_op_negx_16_pd(); break;
            case 612: m68k_op_negx_16_di(); break;
            case 613: m68k_op_negx_16_ix(); break;
            case 614: m68k_op_negx_16_aw(); break;
            case 615: m68k_op_negx_16_al(); break;
            case 616: m68k_op_negx_32_d(); break;
            case 617: m68k_op_negx_32_ai(); break;
            case 618: m68k_op_negx_32_pi(); break;
            case 619: m68k_op_negx_32_pd(); break;
            case 620: m68k_op_negx_32_di(); break;
            case 621: m68k_op_negx_32_ix(); break;
            case 622: m68k_op_negx_32_aw(); break;
            case 623: m68k_op_negx_32_al(); break;
            case 624: m68k_op_move_16_frs_d(); break;
            case 625: m68k_op_move_16_frs_ai(); break;
            case 626: m68k_op_move_16_frs_pi(); break;
            case 627: m68k_op_move_16_frs_pd(); break;
            case 628: m68k_op_move_16_frs_di(); break;
            case 629: m68k_op_move_16_frs_ix(); break;
            case 630: m68k_op_move_16_frs_aw(); break;
            case 631: m68k_op_move_16_frs_al(); break;
            case 632: m68k_op_chk_16_d(); break;
            case 633: m68k_op_chk_16_ai(); break;
            case 634: m68k_op_chk_16_pi(); break;
            case 635: m68k_op_chk_16_pd(); break;
            case 636: m68k_op_chk_16_di(); break;
            case 637: m68k_op_chk_16_ix(); break;
            case 638: m68k_op_chk_16_aw(); break;
            case 639: m68k_op_chk_16_al(); break;
            case 640: m68k_op_chk_16_pcdi(); break;
            case 641: m68k_op_chk_16_pcix(); break;
            case 642: m68k_op_chk_16_i(); break;
            case 643: m68k_op_lea_32_ai(); break;
            case 644: m68k_op_lea_32_di(); break;
            case 645: m68k_op_lea_32_ix(); break;
            case 646: m68k_op_lea_32_aw(); break;
            case 647: m68k_op_lea_32_al(); break;
            case 648: m68k_op_lea_32_pcdi(); break;
            case 649: m68k_op_lea_32_pcix(); break;
            case 650: m68k_op_clr_8_d(); break;
            case 651: m68k_op_clr_8_ai(); break;
            case 652: m68k_op_clr_8_pi(); break;
            case 653: m68k_op_clr_8_pi7(); break;
            case 654: m68k_op_clr_8_pd(); break;
            case 655: m68k_op_clr_8_pd7(); break;
            case 656: m68k_op_clr_8_di(); break;
            case 657: m68k_op_clr_8_ix(); break;
            case 658: m68k_op_clr_8_aw(); break;
            case 659: m68k_op_clr_8_al(); break;
            case 660: m68k_op_clr_16_d(); break;
            case 661: m68k_op_clr_16_ai(); break;
            case 662: m68k_op_clr_16_pi(); break;
            case 663: m68k_op_clr_16_pd(); break;
            case 664: m68k_op_clr_16_di(); break;
            case 665: m68k_op_clr_16_ix(); break;
            case 666: m68k_op_clr_16_aw(); break;
            case 667: m68k_op_clr_16_al(); break;
            case 668: m68k_op_clr_32_d(); break;
            case 669: m68k_op_clr_32_ai(); break;
            case 670: m68k_op_clr_32_pi(); break;
            case 671: m68k_op_clr_32_pd(); break;
            case 672: m68k_op_clr_32_di(); break;
            case 673: m68k_op_clr_32_ix(); break;
            case 674: m68k_op_clr_32_aw(); break;
            case 675: m68k_op_clr_32_al(); break;
            case 676: m68k_op_neg_8_d(); break;
            case 677: m68k_op_neg_8_ai(); break;
            case 678: m68k_op_neg_8_pi(); break;
            case 679: m68k_op_neg_8_pi7(); break;
            case 680: m68k_op_neg_8_pd(); break;
            case 681: m68k_op_neg_8_pd7(); break;
            case 682: m68k_op_neg_8_di(); break;
            case 683: m68k_op_neg_8_ix(); break;
            case 684: m68k_op_neg_8_aw(); break;
            case 685: m68k_op_neg_8_al(); break;
            case 686: m68k_op_neg_16_d(); break;
            case 687: m68k_op_neg_16_ai(); break;
            case 688: m68k_op_neg_16_pi(); break;
            case 689: m68k_op_neg_16_pd(); break;
            case 690: m68k_op_neg_16_di(); break;
            case 691: m68k_op_neg_16_ix(); break;
            case 692: m68k_op_neg_16_aw(); break;
            case 693: m68k_op_neg_16_al(); break;
            case 694: m68k_op_neg_32_d(); break;
            case 695: m68k_op_neg_32_ai(); break;
            case 696: m68k_op_neg_32_pi(); break;
            case 697: m68k_op_neg_32_pd(); break;
            case 698: m68k_op_neg_32_di(); break;
            case 699: m68k_op_neg_32_ix(); break;
            case 700: m68k_op_neg_32_aw(); break;
            case 701: m68k_op_neg_32_al(); break;
            case 702: m68k_op_move_16_toc_d(); break;
            case 703: m68k_op_move_16_toc_ai(); break;
            case 704: m68k_op_move_16_toc_pi(); break;
            case 705: m68k_op_move_16_toc_pd(); break;
            case 706: m68k_op_move_16_toc_di(); break;
            case 707: m68k_op_move_16_toc_ix(); break;
            case 708: m68k_op_move_16_toc_aw(); break;
            case 709: m68k_op_move_16_toc_al(); break;
            case 710: m68k_op_move_16_toc_pcdi(); break;
            case 711: m68k_op_move_16_toc_pcix(); break;
            case 712: m68k_op_move_16_toc_i(); break;
            case 713: m68k_op_not_8_d(); break;
            case 714: m68k_op_not_8_ai(); break;
            case 715: m68k_op_not_8_pi(); break;
            case 716: m68k_op_not_8_pi7(); break;
            case 717: m68k_op_not_8_pd(); break;
            case 718: m68k_op_not_8_pd7(); break;
            case 719: m68k_op_not_8_di(); break;
            case 720: m68k_op_not_8_ix(); break;
            case 721: m68k_op_not_8_aw(); break;
            case 722: m68k_op_not_8_al(); break;
            case 723: m68k_op_not_16_d(); break;
            case 724: m68k_op_not_16_ai(); break;
            case 725: m68k_op_not_16_pi(); break;
            case 726: m68k_op_not_16_pd(); break;
            case 727: m68k_op_not_16_di(); break;
            case 728: m68k_op_not_16_ix(); break;
            case 729: m68k_op_not_16_aw(); break;
            case 730: m68k_op_not_16_al(); break;
            case 731: m68k_op_not_32_d(); break;
            case 732: m68k_op_not_32_ai(); break;
            case 733: m68k_op_not_32_pi(); break;
            case 734: m68k_op_not_32_pd(); break;
            case 735: m68k_op_not_32_di(); break;
            case 736: m68k_op_not_32_ix(); break;
            case 737: m68k_op_not_32_aw(); break;
            case 738: m68k_op_not_32_al(); break;
            case 739: m68k_op_move_16_tos_d(); break;
            case 740: m68k_op_move_16_tos_ai(); break;
            case 741: m68k_op_move_16_tos_pi(); break;
            case 742: m68k_op_move_16_tos_pd(); break;
            case 743: m68k_op_move_16_tos_di(); break;
            case 744: m68k_op_move_16_tos_ix(); break;
            case 745: m68k_op_move_16_tos_aw(); break;
            case 746: m68k_op_move_16_tos_al(); break;
            case 747: m68k_op_move_16_tos_pcdi(); break;
            case 748: m68k_op_move_16_tos_pcix(); break;
            case 749: m68k_op_move_16_tos_i(); break;
            case 750: m68k_op_nbcd_8_d(); break;
            case 751: m68k_op_nbcd_8_ai(); break;
            case 752: m68k_op_nbcd_8_pi(); break;
            case 753: m68k_op_nbcd_8_pi7(); break;
            case 754: m68k_op_nbcd_8_pd(); break;
            case 755: m68k_op_nbcd_8_pd7(); break;
            case 756: m68k_op_nbcd_8_di(); break;
            case 757: m68k_op_nbcd_8_ix(); break;
            case 758: m68k_op_nbcd_8_aw(); break;
            case 759: m68k_op_nbcd_8_al(); break;
            case 760: m68k_op_swap_32(); break;
            case 761: m68k_op_pea_32_ai(); break;
            case 762: m68k_op_pea_32_di(); break;
            case 763: m68k_op_pea_32_ix(); break;
            case 764: m68k_op_pea_32_aw(); break;
            case 765: m68k_op_pea_32_al(); break;
            case 766: m68k_op_pea_32_pcdi(); break;
            case 767: m68k_op_pea_32_pcix(); break;
            case 768: m68k_op_ext_16(); break;
            case 769: m68k_op_movem_16_re_ai(); break;
            case 770: m68k_op_movem_16_re_pd(); break;
            case 771: m68k_op_movem_16_re_di(); break;
            case 772: m68k_op_movem_16_re_ix(); break;
            case 773: m68k_op_movem_16_re_aw(); break;
            case 774: m68k_op_movem_16_re_al(); break;
            case 775: m68k_op_ext_32(); break;
            case 776: m68k_op_movem_32_re_ai(); break;
            case 777: m68k_op_movem_32_re_pd(); break;
            case 778: m68k_op_movem_32_re_di(); break;
            case 779: m68k_op_movem_32_re_ix(); break;
            case 780: m68k_op_movem_32_re_aw(); break;
            case 781: m68k_op_movem_32_re_al(); break;
            case 782: m68k_op_tst_8_d(); break;
            case 783: m68k_op_tst_8_ai(); break;
            case 784: m68k_op_tst_8_pi(); break;
            case 785: m68k_op_tst_8_pi7(); break;
            case 786: m68k_op_tst_8_pd(); break;
            case 787: m68k_op_tst_8_pd7(); break;
            case 788: m68k_op_tst_8_di(); break;
            case 789: m68k_op_tst_8_ix(); break;
            case 790: m68k_op_tst_8_aw(); break;
            case 791: m68k_op_tst_8_al(); break;
            case 792: m68k_op_tst_16_d(); break;
            case 793: m68k_op_tst_16_ai(); break;
            case 794: m68k_op_tst_16_pi(); break;
            case 795: m68k_op_tst_16_pd(); break;
            case 796: m68k_op_tst_16_di(); break;
            case 797: m68k_op_tst_16_ix(); break;
            case 798: m68k_op_tst_16_aw(); break;
            case 799: m68k_op_tst_16_al(); break;
            case 800: m68k_op_tst_32_d(); break;
            case 801: m68k_op_tst_32_ai(); break;
            case 802: m68k_op_tst_32_pi(); break;
            case 803: m68k_op_tst_32_pd(); break;
            case 804: m68k_op_tst_32_di(); break;
            case 805: m68k_op_tst_32_ix(); break;
            case 806: m68k_op_tst_32_aw(); break;
            case 807: m68k_op_tst_32_al(); break;
            case 808: m68k_op_tas_8_d(); break;
            case 809: m68k_op_tas_8_ai(); break;
            case 810: m68k_op_tas_8_pi(); break;
            case 811: m68k_op_tas_8_pi7(); break;
            case 812: m68k_op_tas_8_pd(); break;
            case 813: m68k_op_tas_8_pd7(); break;
            case 814: m68k_op_tas_8_di(); break;
            case 815: m68k_op_tas_8_ix(); break;
            case 816: m68k_op_tas_8_aw(); break;
            case 817: m68k_op_tas_8_al(); break;
            case 818: m68k_op_movem_16_er_ai(); break;
            case 819: m68k_op_movem_16_er_pi(); break;
            case 820: m68k_op_movem_16_er_di(); break;
            case 821: m68k_op_movem_16_er_ix(); break;
            case 822: m68k_op_movem_16_er_aw(); break;
            case 823: m68k_op_movem_16_er_al(); break;
            case 824: m68k_op_movem_16_er_pcdi(); break;
            case 825: m68k_op_movem_16_er_pcix(); break;
            case 826: m68k_op_movem_32_er_ai(); break;
            case 827: m68k_op_movem_32_er_pi(); break;
            case 828: m68k_op_movem_32_er_di(); break;
            case 829: m68k_op_movem_32_er_ix(); break;
            case 830: m68k_op_movem_32_er_aw(); break;
            case 831: m68k_op_movem_32_er_al(); break;
            case 832: m68k_op_movem_32_er_pcdi(); break;
            case 833: m68k_op_movem_32_er_pcix(); break;
            case 834: m68k_op_trap(); break;
            case 835: m68k_op_link_16(); break;
            case 836: m68k_op_link_16_a7(); break;
            case 837: m68k_op_unlk_32(); break;
            case 838: m68k_op_unlk_32_a7(); break;
            case 839: m68k_op_move_32_tou(); break;
            case 840: m68k_op_move_32_fru(); break;
            case 841: m68k_op_reset(); break;
            case 842: m68k_op_nop(); break;
            case 843: m68k_op_stop(); break;
            case 844: m68k_op_rte_32(); break;
            case 845: m68k_op_rts_32(); break;
            case 846: m68k_op_trapv(); break;
            case 847: m68k_op_rtr_32(); break;
            case 848: m68k_op_jsr_32_ai(); break;
            case 849: m68k_op_jsr_32_di(); break;
            case 850: m68k_op_jsr_32_ix(); break;
            case 851: m68k_op_jsr_32_aw(); break;
            case 852: m68k_op_jsr_32_al(); break;
            case 853: m68k_op_jsr_32_pcdi(); break;
            case 854: m68k_op_jsr_32_pcix(); break;
            case 855: m68k_op_jmp_32_ai(); break;
            case 856: m68k_op_jmp_32_di(); break;
            case 857: m68k_op_jmp_32_ix(); break;
            case 858: m68k_op_jmp_32_aw(); break;
            case 859: m68k_op_jmp_32_al(); break;
            case 860: m68k_op_jmp_32_pcdi(); break;
            case 861: m68k_op_jmp_32_pcix(); break;
            case 862: m68k_op_addq_8_d(); break;
            case 863: m68k_op_addq_8_ai(); break;
            case 864: m68k_op_addq_8_pi(); break;
            case 865: m68k_op_addq_8_pi7(); break;
            case 866: m68k_op_addq_8_pd(); break;
            case 867: m68k_op_addq_8_pd7(); break;
            case 868: m68k_op_addq_8_di(); break;
            case 869: m68k_op_addq_8_ix(); break;
            case 870: m68k_op_addq_8_aw(); break;
            case 871: m68k_op_addq_8_al(); break;
            case 872: m68k_op_addq_16_d(); break;
            case 873: m68k_op_addq_16_a(); break;
            case 874: m68k_op_addq_16_ai(); break;
            case 875: m68k_op_addq_16_pi(); break;
            case 876: m68k_op_addq_16_pd(); break;
            case 877: m68k_op_addq_16_di(); break;
            case 878: m68k_op_addq_16_ix(); break;
            case 879: m68k_op_addq_16_aw(); break;
            case 880: m68k_op_addq_16_al(); break;
            case 881: m68k_op_addq_32_d(); break;
            case 882: m68k_op_addq_32_a(); break;
            case 883: m68k_op_addq_32_ai(); break;
            case 884: m68k_op_addq_32_pi(); break;
            case 885: m68k_op_addq_32_pd(); break;
            case 886: m68k_op_addq_32_di(); break;
            case 887: m68k_op_addq_32_ix(); break;
            case 888: m68k_op_addq_32_aw(); break;
            case 889: m68k_op_addq_32_al(); break;
            case 890: m68k_op_st_8_d(); break;
            case 891: m68k_op_dbt_16(); break;
            case 892: m68k_op_st_8_ai(); break;
            case 893: m68k_op_st_8_pi(); break;
            case 894: m68k_op_st_8_pi7(); break;
            case 895: m68k_op_st_8_pd(); break;
            case 896: m68k_op_st_8_pd7(); break;
            case 897: m68k_op_st_8_di(); break;
            case 898: m68k_op_st_8_ix(); break;
            case 899: m68k_op_st_8_aw(); break;
            case 900: m68k_op_st_8_al(); break;
            case 901: m68k_op_subq_8_d(); break;
            case 902: m68k_op_subq_8_ai(); break;
            case 903: m68k_op_subq_8_pi(); break;
            case 904: m68k_op_subq_8_pi7(); break;
            case 905: m68k_op_subq_8_pd(); break;
            case 906: m68k_op_subq_8_pd7(); break;
            case 907: m68k_op_subq_8_di(); break;
            case 908: m68k_op_subq_8_ix(); break;
            case 909: m68k_op_subq_8_aw(); break;
            case 910: m68k_op_subq_8_al(); break;
            case 911: m68k_op_subq_16_d(); break;
            case 912: m68k_op_subq_16_a(); break;
            case 913: m68k_op_subq_16_ai(); break;
            case 914: m68k_op_subq_16_pi(); break;
            case 915: m68k_op_subq_16_pd(); break;
            case 916: m68k_op_subq_16_di(); break;
            case 917: m68k_op_subq_16_ix(); break;
            case 918: m68k_op_subq_16_aw(); break;
            case 919: m68k_op_subq_16_al(); break;
            case 920: m68k_op_subq_32_d(); break;
            case 921: m68k_op_subq_32_a(); break;
            case 922: m68k_op_subq_32_ai(); break;
            case 923: m68k_op_subq_32_pi(); break;
            case 924: m68k_op_subq_32_pd(); break;
            case 925: m68k_op_subq_32_di(); break;
            case 926: m68k_op_subq_32_ix(); break;
            case 927: m68k_op_subq_32_aw(); break;
            case 928: m68k_op_subq_32_al(); break;
            case 929: m68k_op_sf_8_d(); break;
            case 930: m68k_op_dbf_16(); break;
            case 931: m68k_op_sf_8_ai(); break;
            case 932: m68k_op_sf_8_pi(); break;
            case 933: m68k_op_sf_8_pi7(); break;
            case 934: m68k_op_sf_8_pd(); break;
            case 935: m68k_op_sf_8_pd7(); break;
            case 936: m68k_op_sf_8_di(); break;
            case 937: m68k_op_sf_8_ix(); break;
            case 938: m68k_op_sf_8_aw(); break;
            case 939: m68k_op_sf_8_al(); break;
            case 940: m68k_op_shi_8_d(); break;
            case 941: m68k_op_dbhi_16(); break;
            case 942: m68k_op_shi_8_ai(); break;
            case 943: m68k_op_shi_8_pi(); break;
            case 944: m68k_op_shi_8_pi7(); break;
            case 945: m68k_op_shi_8_pd(); break;
            case 946: m68k_op_shi_8_pd7(); break;
            case 947: m68k_op_shi_8_di(); break;
            case 948: m68k_op_shi_8_ix(); break;
            case 949: m68k_op_shi_8_aw(); break;
            case 950: m68k_op_shi_8_al(); break;
            case 951: m68k_op_sls_8_d(); break;
            case 952: m68k_op_dbls_16(); break;
            case 953: m68k_op_sls_8_ai(); break;
            case 954: m68k_op_sls_8_pi(); break;
            case 955: m68k_op_sls_8_pi7(); break;
            case 956: m68k_op_sls_8_pd(); break;
            case 957: m68k_op_sls_8_pd7(); break;
            case 958: m68k_op_sls_8_di(); break;
            case 959: m68k_op_sls_8_ix(); break;
            case 960: m68k_op_sls_8_aw(); break;
            case 961: m68k_op_sls_8_al(); break;
            case 962: m68k_op_scc_8_d(); break;
            case 963: m68k_op_dbcc_16(); break;
            case 964: m68k_op_scc_8_ai(); break;
            case 965: m68k_op_scc_8_pi(); break;
            case 966: m68k_op_scc_8_pi7(); break;
            case 967: m68k_op_scc_8_pd(); break;
            case 968: m68k_op_scc_8_pd7(); break;
            case 969: m68k_op_scc_8_di(); break;
            case 970: m68k_op_scc_8_ix(); break;
            case 971: m68k_op_scc_8_aw(); break;
            case 972: m68k_op_scc_8_al(); break;
            case 973: m68k_op_scs_8_d(); break;
            case 974: m68k_op_dbcs_16(); break;
            case 975: m68k_op_scs_8_ai(); break;
            case 976: m68k_op_scs_8_pi(); break;
            case 977: m68k_op_scs_8_pi7(); break;
            case 978: m68k_op_scs_8_pd(); break;
            case 979: m68k_op_scs_8_pd7(); break;
            case 980: m68k_op_scs_8_di(); break;
            case 981: m68k_op_scs_8_ix(); break;
            case 982: m68k_op_scs_8_aw(); break;
            case 983: m68k_op_scs_8_al(); break;
            case 984: m68k_op_sne_8_d(); break;
            case 985: m68k_op_dbne_16(); break;
            case 986: m68k_op_sne_8_ai(); break;
            case 987: m68k_op_sne_8_pi(); break;
            case 988: m68k_op_sne_8_pi7(); break;
            case 989: m68k_op_sne_8_pd(); break;
            case 990: m68k_op_sne_8_pd7(); break;
            case 991: m68k_op_sne_8_di(); break;
            case 992: m68k_op_sne_8_ix(); break;
            case 993: m68k_op_sne_8_aw(); break;
            case 994: m68k_op_sne_8_al(); break;
            case 995: m68k_op_seq_8_d(); break;
            case 996: m68k_op_dbeq_16(); break;
            case 997: m68k_op_seq_8_ai(); break;
            case 998: m68k_op_seq_8_pi(); break;
            case 999: m68k_op_seq_8_pi7(); break;
            case 1000: m68k_op_seq_8_pd(); break;
            case 1001: m68k_op_seq_8_pd7(); break;
            case 1002: m68k_op_seq_8_di(); break;
            case 1003: m68k_op_seq_8_ix(); break;
            case 1004: m68k_op_seq_8_aw(); break;
            case 1005: m68k_op_seq_8_al(); break;
            case 1006: m68k_op_svc_8_d(); break;
            case 1007: m68k_op_dbvc_16(); break;
            case 1008: m68k_op_svc_8_ai(); break;
            case 1009: m68k_op_svc_8_pi(); break;
            case 1010: m68k_op_svc_8_pi7(); break;
            case 1011: m68k_op_svc_8_pd(); break;
            case 1012: m68k_op_svc_8_pd7(); break;
            case 1013: m68k_op_svc_8_di(); break;
            case 1014: m68k_op_svc_8_ix(); break;
            case 1015: m68k_op_svc_8_aw(); break;
            case 1016: m68k_op_svc_8_al(); break;
            case 1017: m68k_op_svs_8_d(); break;
            case 1018: m68k_op_dbvs_16(); break;
            case 1019: m68k_op_svs_8_ai(); break;
            case 1020: m68k_op_svs_8_pi(); break;
            case 1021: m68k_op_svs_8_pi7(); break;
            case 1022: m68k_op_svs_8_pd(); break;
            case 1023: m68k_op_svs_8_pd7(); break;
            case 1024: m68k_op_svs_8_di(); break;
            case 1025: m68k_op_svs_8_ix(); break;
            case 1026: m68k_op_svs_8_aw(); break;
            case 1027: m68k_op_svs_8_al(); break;
            case 1028: m68k_op_spl_8_d(); break;
            case 1029: m68k_op_dbpl_16(); break;
            case 1030: m68k_op_spl_8_ai(); break;
            case 1031: m68k_op_spl_8_pi(); break;
            case 1032: m68k_op_spl_8_pi7(); break;
            case 1033: m68k_op_spl_8_pd(); break;
            case 1034: m68k_op_spl_8_pd7(); break;
            case 1035: m68k_op_spl_8_di(); break;
            case 1036: m68k_op_spl_8_ix(); break;
            case 1037: m68k_op_spl_8_aw(); break;
            case 1038: m68k_op_spl_8_al(); break;
            case 1039: m68k_op_smi_8_d(); break;
            case 1040: m68k_op_dbmi_16(); break;
            case 1041: m68k_op_smi_8_ai(); break;
            case 1042: m68k_op_smi_8_pi(); break;
            case 1043: m68k_op_smi_8_pi7(); break;
            case 1044: m68k_op_smi_8_pd(); break;
            case 1045: m68k_op_smi_8_pd7(); break;
            case 1046: m68k_op_smi_8_di(); break;
            case 1047: m68k_op_smi_8_ix(); break;
            case 1048: m68k_op_smi_8_aw(); break;
            case 1049: m68k_op_smi_8_al(); break;
            case 1050: m68k_op_sge_8_d(); break;
            case 1051: m68k_op_dbge_16(); break;
            case 1052: m68k_op_sge_8_ai(); break;
            case 1053: m68k_op_sge_8_pi(); break;
            case 1054: m68k_op_sge_8_pi7(); break;
            case 1055: m68k_op_sge_8_pd(); break;
            case 1056: m68k_op_sge_8_pd7(); break;
            case 1057: m68k_op_sge_8_di(); break;
            case 1058: m68k_op_sge_8_ix(); break;
            case 1059: m68k_op_sge_8_aw(); break;
            case 1060: m68k_op_sge_8_al(); break;
            case 1061: m68k_op_slt_8_d(); break;
            case 1062: m68k_op_dblt_16(); break;
            case 1063: m68k_op_slt_8_ai(); break;
            case 1064: m68k_op_slt_8_pi(); break;
            case 1065: m68k_op_slt_8_pi7(); break;
            case 1066: m68k_op_slt_8_pd(); break;
            case 1067: m68k_op_slt_8_pd7(); break;
            case 1068: m68k_op_slt_8_di(); break;
            case 1069: m68k_op_slt_8_ix(); break;
            case 1070: m68k_op_slt_8_aw(); break;
            case 1071: m68k_op_slt_8_al(); break;
            case 1072: m68k_op_sgt_8_d(); break;
            case 1073: m68k_op_dbgt_16(); break;
            case 1074: m68k_op_sgt_8_ai(); break;
            case 1075: m68k_op_sgt_8_pi(); break;
            case 1076: m68k_op_sgt_8_pi7(); break;
            case 1077: m68k_op_sgt_8_pd(); break;
            case 1078: m68k_op_sgt_8_pd7(); break;
            case 1079: m68k_op_sgt_8_di(); break;
            case 1080: m68k_op_sgt_8_ix(); break;
            case 1081: m68k_op_sgt_8_aw(); break;
            case 1082: m68k_op_sgt_8_al(); break;
            case 1083: m68k_op_sle_8_d(); break;
            case 1084: m68k_op_dble_16(); break;
            case 1085: m68k_op_sle_8_ai(); break;
            case 1086: m68k_op_sle_8_pi(); break;
            case 1087: m68k_op_sle_8_pi7(); break;
            case 1088: m68k_op_sle_8_pd(); break;
            case 1089: m68k_op_sle_8_pd7(); break;
            case 1090: m68k_op_sle_8_di(); break;
            case 1091: m68k_op_sle_8_ix(); break;
            case 1092: m68k_op_sle_8_aw(); break;
            case 1093: m68k_op_sle_8_al(); break;
            case 1094: m68k_op_bra_16(); break;
            case 1095: m68k_op_bra_8(); break;
            case 1096: m68k_op_bra_32(); break;
            case 1097: m68k_op_bsr_16(); break;
            case 1098: m68k_op_bsr_8(); break;
            case 1099: m68k_op_bsr_32(); break;
            case 1100: m68k_op_bhi_16(); break;
            case 1101: m68k_op_bhi_8(); break;
            case 1102: m68k_op_bhi_32(); break;
            case 1103: m68k_op_bls_16(); break;
            case 1104: m68k_op_bls_8(); break;
            case 1105: m68k_op_bls_32(); break;
            case 1106: m68k_op_bcc_16(); break;
            case 1107: m68k_op_bcc_8(); break;
            case 1108: m68k_op_bcc_32(); break;
            case 1109: m68k_op_bcs_16(); break;
            case 1110: m68k_op_bcs_8(); break;
            case 1111: m68k_op_bcs_32(); break;
            case 1112: m68k_op_bne_16(); break;
            case 1113: m68k_op_bne_8(); break;
            case 1114: m68k_op_bne_32(); break;
            case 1115: m68k_op_beq_16(); break;
            case 1116: m68k_op_beq_8(); break;
            case 1117: m68k_op_beq_32(); break;
            case 1118: m68k_op_bvc_16(); break;
            case 1119: m68k_op_bvc_8(); break;
            case 1120: m68k_op_bvc_32(); break;
            case 1121: m68k_op_bvs_16(); break;
            case 1122: m68k_op_bvs_8(); break;
            case 1123: m68k_op_bvs_32(); break;
            case 1124: m68k_op_bpl_16(); break;
            case 1125: m68k_op_bpl_8(); break;
            case 1126: m68k_op_bpl_32(); break;
            case 1127: m68k_op_bmi_16(); break;
            case 1128: m68k_op_bmi_8(); break;
            case 1129: m68k_op_bmi_32(); break;
            case 1130: m68k_op_bge_16(); break;
            case 1131: m68k_op_bge_8(); break;
            case 1132: m68k_op_bge_32(); break;
            case 1133: m68k_op_blt_16(); break;
            case 1134: m68k_op_blt_8(); break;
            case 1135: m68k_op_blt_32(); break;
            case 1136: m68k_op_bgt_16(); break;
            case 1137: m68k_op_bgt_8(); break;
            case 1138: m68k_op_bgt_32(); break;
            case 1139: m68k_op_ble_16(); break;
            case 1140: m68k_op_ble_8(); break;
            case 1141: m68k_op_ble_32(); break;
            case 1142: m68k_op_moveq_32(); break;
            case 1143: m68k_op_or_8_er_d(); break;
            case 1144: m68k_op_or_8_er_ai(); break;
            case 1145: m68k_op_or_8_er_pi(); break;
            case 1146: m68k_op_or_8_er_pi7(); break;
            case 1147: m68k_op_or_8_er_pd(); break;
            case 1148: m68k_op_or_8_er_pd7(); break;
            case 1149: m68k_op_or_8_er_di(); break;
            case 1150: m68k_op_or_8_er_ix(); break;
            case 1151: m68k_op_or_8_er_aw(); break;
            case 1152: m68k_op_or_8_er_al(); break;
            case 1153: m68k_op_or_8_er_pcdi(); break;
            case 1154: m68k_op_or_8_er_pcix(); break;
            case 1155: m68k_op_or_8_er_i(); break;
            case 1156: m68k_op_or_16_er_d(); break;
            case 1157: m68k_op_or_16_er_ai(); break;
            case 1158: m68k_op_or_16_er_pi(); break;
            case 1159: m68k_op_or_16_er_pd(); break;
            case 1160: m68k_op_or_16_er_di(); break;
            case 1161: m68k_op_or_16_er_ix(); break;
            case 1162: m68k_op_or_16_er_aw(); break;
            case 1163: m68k_op_or_16_er_al(); break;
            case 1164: m68k_op_or_16_er_pcdi(); break;
            case 1165: m68k_op_or_16_er_pcix(); break;
            case 1166: m68k_op_or_16_er_i(); break;
            case 1167: m68k_op_or_32_er_d(); break;
            case 1168: m68k_op_or_32_er_ai(); break;
            case 1169: m68k_op_or_32_er_pi(); break;
            case 1170: m68k_op_or_32_er_pd(); break;
            case 1171: m68k_op_or_32_er_di(); break;
            case 1172: m68k_op_or_32_er_ix(); break;
            case 1173: m68k_op_or_32_er_aw(); break;
            case 1174: m68k_op_or_32_er_al(); break;
            case 1175: m68k_op_or_32_er_pcdi(); break;
            case 1176: m68k_op_or_32_er_pcix(); break;
            case 1177: m68k_op_or_32_er_i(); break;
            case 1178: m68k_op_divu_16_d(); break;
            case 1179: m68k_op_divu_16_ai(); break;
            case 1180: m68k_op_divu_16_pi(); break;
            case 1181: m68k_op_divu_16_pd(); break;
            case 1182: m68k_op_divu_16_di(); break;
            case 1183: m68k_op_divu_16_ix(); break;
            case 1184: m68k_op_divu_16_aw(); break;
            case 1185: m68k_op_divu_16_al(); break;
            case 1186: m68k_op_divu_16_pcdi(); break;
            case 1187: m68k_op_divu_16_pcix(); break;
            case 1188: m68k_op_divu_16_i(); break;
            case 1189: m68k_op_sbcd_8_rr(); break;
            case 1190: m68k_op_sbcd_8_mm(); break;
            case 1191: m68k_op_sbcd_8_mm_ay7(); break;
            case 1192: m68k_op_or_8_re_ai(); break;
            case 1193: m68k_op_or_8_re_pi(); break;
            case 1194: m68k_op_or_8_re_pi7(); break;
            case 1195: m68k_op_or_8_re_pd(); break;
            case 1196: m68k_op_or_8_re_pd7(); break;
            case 1197: m68k_op_or_8_re_di(); break;
            case 1198: m68k_op_or_8_re_ix(); break;
            case 1199: m68k_op_or_8_re_aw(); break;
            case 1200: m68k_op_or_8_re_al(); break;
            case 1201: m68k_op_or_16_re_ai(); break;
            case 1202: m68k_op_or_16_re_pi(); break;
            case 1203: m68k_op_or_16_re_pd(); break;
            case 1204: m68k_op_or_16_re_di(); break;
            case 1205: m68k_op_or_16_re_ix(); break;
            case 1206: m68k_op_or_16_re_aw(); break;
            case 1207: m68k_op_or_16_re_al(); break;
            case 1208: m68k_op_or_32_re_ai(); break;
            case 1209: m68k_op_or_32_re_pi(); break;
            case 1210: m68k_op_or_32_re_pd(); break;
            case 1211: m68k_op_or_32_re_di(); break;
            case 1212: m68k_op_or_32_re_ix(); break;
            case 1213: m68k_op_or_32_re_aw(); break;
            case 1214: m68k_op_or_32_re_al(); break;
            case 1215: m68k_op_divs_16_d(); break;
            case 1216: m68k_op_divs_16_ai(); break;
            case 1217: m68k_op_divs_16_pi(); break;
            case 1218: m68k_op_divs_16_pd(); break;
            case 1219: m68k_op_divs_16_di(); break;
            case 1220: m68k_op_divs_16_ix(); break;
            case 1221: m68k_op_divs_16_aw(); break;
            case 1222: m68k_op_divs_16_al(); break;
            case 1223: m68k_op_divs_16_pcdi(); break;
            case 1224: m68k_op_divs_16_pcix(); break;
            case 1225: m68k_op_divs_16_i(); break;
            case 1226: m68k_op_sbcd_8_mm_ax7(); break;
            case 1227: m68k_op_sbcd_8_mm_axy7(); break;
            case 1228: m68k_op_sub_8_er_d(); break;
            case 1229: m68k_op_sub_8_er_ai(); break;
            case 1230: m68k_op_sub_8_er_pi(); break;
            case 1231: m68k_op_sub_8_er_pi7(); break;
            case 1232: m68k_op_sub_8_er_pd(); break;
            case 1233: m68k_op_sub_8_er_pd7(); break;
            case 1234: m68k_op_sub_8_er_di(); break;
            case 1235: m68k_op_sub_8_er_ix(); break;
            case 1236: m68k_op_sub_8_er_aw(); break;
            case 1237: m68k_op_sub_8_er_al(); break;
            case 1238: m68k_op_sub_8_er_pcdi(); break;
            case 1239: m68k_op_sub_8_er_pcix(); break;
            case 1240: m68k_op_sub_8_er_i(); break;
            case 1241: m68k_op_sub_16_er_d(); break;
            case 1242: m68k_op_sub_16_er_a(); break;
            case 1243: m68k_op_sub_16_er_ai(); break;
            case 1244: m68k_op_sub_16_er_pi(); break;
            case 1245: m68k_op_sub_16_er_pd(); break;
            case 1246: m68k_op_sub_16_er_di(); break;
            case 1247: m68k_op_sub_16_er_ix(); break;
            case 1248: m68k_op_sub_16_er_aw(); break;
            case 1249: m68k_op_sub_16_er_al(); break;
            case 1250: m68k_op_sub_16_er_pcdi(); break;
            case 1251: m68k_op_sub_16_er_pcix(); break;
            case 1252: m68k_op_sub_16_er_i(); break;
            case 1253: m68k_op_sub_32_er_d(); break;
            case 1254: m68k_op_sub_32_er_a(); break;
            case 1255: m68k_op_sub_32_er_ai(); break;
            case 1256: m68k_op_sub_32_er_pi(); break;
            case 1257: m68k_op_sub_32_er_pd(); break;
            case 1258: m68k_op_sub_32_er_di(); break;
            case 1259: m68k_op_sub_32_er_ix(); break;
            case 1260: m68k_op_sub_32_er_aw(); break;
            case 1261: m68k_op_sub_32_er_al(); break;
            case 1262: m68k_op_sub_32_er_pcdi(); break;
            case 1263: m68k_op_sub_32_er_pcix(); break;
            case 1264: m68k_op_sub_32_er_i(); break;
            case 1265: m68k_op_suba_16_d(); break;
            case 1266: m68k_op_suba_16_a(); break;
            case 1267: m68k_op_suba_16_ai(); break;
            case 1268: m68k_op_suba_16_pi(); break;
            case 1269: m68k_op_suba_16_pd(); break;
            case 1270: m68k_op_suba_16_di(); break;
            case 1271: m68k_op_suba_16_ix(); break;
            case 1272: m68k_op_suba_16_aw(); break;
            case 1273: m68k_op_suba_16_al(); break;
            case 1274: m68k_op_suba_16_pcdi(); break;
            case 1275: m68k_op_suba_16_pcix(); break;
            case 1276: m68k_op_suba_16_i(); break;
            case 1277: m68k_op_subx_8_rr(); break;
            case 1278: m68k_op_subx_8_mm(); break;
            case 1279: m68k_op_subx_8_mm_ay7(); break;
            case 1280: m68k_op_sub_8_re_ai(); break;
            case 1281: m68k_op_sub_8_re_pi(); break;
            case 1282: m68k_op_sub_8_re_pi7(); break;
            case 1283: m68k_op_sub_8_re_pd(); break;
            case 1284: m68k_op_sub_8_re_pd7(); break;
            case 1285: m68k_op_sub_8_re_di(); break;
            case 1286: m68k_op_sub_8_re_ix(); break;
            case 1287: m68k_op_sub_8_re_aw(); break;
            case 1288: m68k_op_sub_8_re_al(); break;
            case 1289: m68k_op_subx_16_rr(); break;
            case 1290: m68k_op_subx_16_mm(); break;
            case 1291: m68k_op_sub_16_re_ai(); break;
            case 1292: m68k_op_sub_16_re_pi(); break;
            case 1293: m68k_op_sub_16_re_pd(); break;
            case 1294: m68k_op_sub_16_re_di(); break;
            case 1295: m68k_op_sub_16_re_ix(); break;
            case 1296: m68k_op_sub_16_re_aw(); break;
            case 1297: m68k_op_sub_16_re_al(); break;
            case 1298: m68k_op_subx_32_rr(); break;
            case 1299: m68k_op_subx_32_mm(); break;
            case 1300: m68k_op_sub_32_re_ai(); break;
            case 1301: m68k_op_sub_32_re_pi(); break;
            case 1302: m68k_op_sub_32_re_pd(); break;
            case 1303: m68k_op_sub_32_re_di(); break;
            case 1304: m68k_op_sub_32_re_ix(); break;
            case 1305: m68k_op_sub_32_re_aw(); break;
            case 1306: m68k_op_sub_32_re_al(); break;
            case 1307: m68k_op_suba_32_d(); break;
            case 1308: m68k_op_suba_32_a(); break;
            case 1309: m68k_op_suba_32_ai(); break;
            case 1310: m68k_op_suba_32_pi(); break;
            case 1311: m68k_op_suba_32_pd(); break;
            case 1312: m68k_op_suba_32_di(); break;
            case 1313: m68k_op_suba_32_ix(); break;
            case 1314: m68k_op_suba_32_aw(); break;
            case 1315: m68k_op_suba_32_al(); break;
            case 1316: m68k_op_suba_32_pcdi(); break;
            case 1317: m68k_op_suba_32_pcix(); break;
            case 1318: m68k_op_suba_32_i(); break;
            case 1319: m68k_op_subx_8_mm_ax7(); break;
            case 1320: m68k_op_subx_8_mm_axy7(); break;
            case 1321: m68k_op_1010(); break;
            case 1322: m68k_op_cmp_8_d(); break;
            case 1323: m68k_op_cmp_8_ai(); break;
            case 1324: m68k_op_cmp_8_pi(); break;
            case 1325: m68k_op_cmp_8_pi7(); break;
            case 1326: m68k_op_cmp_8_pd(); break;
            case 1327: m68k_op_cmp_8_pd7(); break;
            case 1328: m68k_op_cmp_8_di(); break;
            case 1329: m68k_op_cmp_8_ix(); break;
            case 1330: m68k_op_cmp_8_aw(); break;
            case 1331: m68k_op_cmp_8_al(); break;
            case 1332: m68k_op_cmp_8_pcdi(); break;
            case 1333: m68k_op_cmp_8_pcix(); break;
            case 1334: m68k_op_cmp_8_i(); break;
            case 1335: m68k_op_cmp_16_d(); break;
            case 1336: m68k_op_cmp_16_a(); break;
            case 1337: m68k_op_cmp_16_ai(); break;
            case 1338: m68k_op_cmp_16_pi(); break;
            case 1339: m68k_op_cmp_16_pd(); break;
            case 1340: m68k_op_cmp_16_di(); break;
            case 1341: m68k_op_cmp_16_ix(); break;
            case 1342: m68k_op_cmp_16_aw(); break;
            case 1343: m68k_op_cmp_16_al(); break;
            case 1344: m68k_op_cmp_16_pcdi(); break;
            case 1345: m68k_op_cmp_16_pcix(); break;
            case 1346: m68k_op_cmp_16_i(); break;
            case 1347: m68k_op_cmp_32_d(); break;
            case 1348: m68k_op_cmp_32_a(); break;
            case 1349: m68k_op_cmp_32_ai(); break;
            case 1350: m68k_op_cmp_32_pi(); break;
            case 1351: m68k_op_cmp_32_pd(); break;
            case 1352: m68k_op_cmp_32_di(); break;
            case 1353: m68k_op_cmp_32_ix(); break;
            case 1354: m68k_op_cmp_32_aw(); break;
            case 1355: m68k_op_cmp_32_al(); break;
            case 1356: m68k_op_cmp_32_pcdi(); break;
            case 1357: m68k_op_cmp_32_pcix(); break;
            case 1358: m68k_op_cmp_32_i(); break;
            case 1359: m68k_op_cmpa_16_d(); break;
            case 1360: m68k_op_cmpa_16_a(); break;
            case 1361: m68k_op_cmpa_16_ai(); break;
            case 1362: m68k_op_cmpa_16_pi(); break;
            case 1363: m68k_op_cmpa_16_pd(); break;
            case 1364: m68k_op_cmpa_16_di(); break;
            case 1365: m68k_op_cmpa_16_ix(); break;
            case 1366: m68k_op_cmpa_16_aw(); break;
            case 1367: m68k_op_cmpa_16_al(); break;
            case 1368: m68k_op_cmpa_16_pcdi(); break;
            case 1369: m68k_op_cmpa_16_pcix(); break;
            case 1370: m68k_op_cmpa_16_i(); break;
            case 1371: m68k_op_eor_8_d(); break;
            case 1372: m68k_op_cmpm_8(); break;
            case 1373: m68k_op_cmpm_8_ay7(); break;
            case 1374: m68k_op_eor_8_ai(); break;
            case 1375: m68k_op_eor_8_pi(); break;
            case 1376: m68k_op_eor_8_pi7(); break;
            case 1377: m68k_op_eor_8_pd(); break;
            case 1378: m68k_op_eor_8_pd7(); break;
            case 1379: m68k_op_eor_8_di(); break;
            case 1380: m68k_op_eor_8_ix(); break;
            case 1381: m68k_op_eor_8_aw(); break;
            case 1382: m68k_op_eor_8_al(); break;
            case 1383: m68k_op_eor_16_d(); break;
            case 1384: m68k_op_cmpm_16(); break;
            case 1385: m68k_op_eor_16_ai(); break;
            case 1386: m68k_op_eor_16_pi(); break;
            case 1387: m68k_op_eor_16_pd(); break;
            case 1388: m68k_op_eor_16_di(); break;
            case 1389: m68k_op_eor_16_ix(); break;
            case 1390: m68k_op_eor_16_aw(); break;
            case 1391: m68k_op_eor_16_al(); break;
            case 1392: m68k_op_eor_32_d(); break;
            case 1393: m68k_op_cmpm_32(); break;
            case 1394: m68k_op_eor_32_ai(); break;
            case 1395: m68k_op_eor_32_pi(); break;
            case 1396: m68k_op_eor_32_pd(); break;
            case 1397: m68k_op_eor_32_di(); break;
            case 1398: m68k_op_eor_32_ix(); break;
            case 1399: m68k_op_eor_32_aw(); break;
            case 1400: m68k_op_eor_32_al(); break;
            case 1401: m68k_op_cmpa_32_d(); break;
            case 1402: m68k_op_cmpa_32_a(); break;
            case 1403: m68k_op_cmpa_32_ai(); break;
            case 1404: m68k_op_cmpa_32_pi(); break;
            case 1405: m68k_op_cmpa_32_pd(); break;
            case 1406: m68k_op_cmpa_32_di(); break;
            case 1407: m68k_op_cmpa_32_ix(); break;
            case 1408: m68k_op_cmpa_32_aw(); break;
            case 1409: m68k_op_cmpa_32_al(); break;
            case 1410: m68k_op_cmpa_32_pcdi(); break;
            case 1411: m68k_op_cmpa_32_pcix(); break;
            case 1412: m68k_op_cmpa_32_i(); break;
            case 1413: m68k_op_cmpm_8_ax7(); break;
            case 1414: m68k_op_cmpm_8_axy7(); break;
            case 1415: m68k_op_and_8_er_d(); break;
            case 1416: m68k_op_and_8_er_ai(); break;
            case 1417: m68k_op_and_8_er_pi(); break;
            case 1418: m68k_op_and_8_er_pi7(); break;
            case 1419: m68k_op_and_8_er_pd(); break;
            case 1420: m68k_op_and_8_er_pd7(); break;
            case 1421: m68k_op_and_8_er_di(); break;
            case 1422: m68k_op_and_8_er_ix(); break;
            case 1423: m68k_op_and_8_er_aw(); break;
            case 1424: m68k_op_and_8_er_al(); break;
            case 1425: m68k_op_and_8_er_pcdi(); break;
            case 1426: m68k_op_and_8_er_pcix(); break;
            case 1427: m68k_op_and_8_er_i(); break;
            case 1428: m68k_op_and_16_er_d(); break;
            case 1429: m68k_op_and_16_er_ai(); break;
            case 1430: m68k_op_and_16_er_pi(); break;
            case 1431: m68k_op_and_16_er_pd(); break;
            case 1432: m68k_op_and_16_er_di(); break;
            case 1433: m68k_op_and_16_er_ix(); break;
            case 1434: m68k_op_and_16_er_aw(); break;
            case 1435: m68k_op_and_16_er_al(); break;
            case 1436: m68k_op_and_16_er_pcdi(); break;
            case 1437: m68k_op_and_16_er_pcix(); break;
            case 1438: m68k_op_and_16_er_i(); break;
            case 1439: m68k_op_and_32_er_d(); break;
            case 1440: m68k_op_and_32_er_ai(); break;
            case 1441: m68k_op_and_32_er_pi(); break;
            case 1442: m68k_op_and_32_er_pd(); break;
            case 1443: m68k_op_and_32_er_di(); break;
            case 1444: m68k_op_and_32_er_ix(); break;
            case 1445: m68k_op_and_32_er_aw(); break;
            case 1446: m68k_op_and_32_er_al(); break;
            case 1447: m68k_op_and_32_er_pcdi(); break;
            case 1448: m68k_op_and_32_er_pcix(); break;
            case 1449: m68k_op_and_32_er_i(); break;
            case 1450: m68k_op_mulu_16_d(); break;
            case 1451: m68k_op_mulu_16_ai(); break;
            case 1452: m68k_op_mulu_16_pi(); break;
            case 1453: m68k_op_mulu_16_pd(); break;
            case 1454: m68k_op_mulu_16_di(); break;
            case 1455: m68k_op_mulu_16_ix(); break;
            case 1456: m68k_op_mulu_16_aw(); break;
            case 1457: m68k_op_mulu_16_al(); break;
            case 1458: m68k_op_mulu_16_pcdi(); break;
            case 1459: m68k_op_mulu_16_pcix(); break;
            case 1460: m68k_op_mulu_16_i(); break;
            case 1461: m68k_op_abcd_8_rr(); break;
            case 1462: m68k_op_abcd_8_mm(); break;
            case 1463: m68k_op_abcd_8_mm_ay7(); break;
            case 1464: m68k_op_and_8_re_ai(); break;
            case 1465: m68k_op_and_8_re_pi(); break;
            case 1466: m68k_op_and_8_re_pi7(); break;
            case 1467: m68k_op_and_8_re_pd(); break;
            case 1468: m68k_op_and_8_re_pd7(); break;
            case 1469: m68k_op_and_8_re_di(); break;
            case 1470: m68k_op_and_8_re_ix(); break;
            case 1471: m68k_op_and_8_re_aw(); break;
            case 1472: m68k_op_and_8_re_al(); break;
            case 1473: m68k_op_exg_32_dd(); break;
            case 1474: m68k_op_exg_32_aa(); break;
            case 1475: m68k_op_and_16_re_ai(); break;
            case 1476: m68k_op_and_16_re_pi(); break;
            case 1477: m68k_op_and_16_re_pd(); break;
            case 1478: m68k_op_and_16_re_di(); break;
            case 1479: m68k_op_and_16_re_ix(); break;
            case 1480: m68k_op_and_16_re_aw(); break;
            case 1481: m68k_op_and_16_re_al(); break;
            case 1482: m68k_op_exg_32_da(); break;
            case 1483: m68k_op_and_32_re_ai(); break;
            case 1484: m68k_op_and_32_re_pi(); break;
            case 1485: m68k_op_and_32_re_pd(); break;
            case 1486: m68k_op_and_32_re_di(); break;
            case 1487: m68k_op_and_32_re_ix(); break;
            case 1488: m68k_op_and_32_re_aw(); break;
            case 1489: m68k_op_and_32_re_al(); break;
            case 1490: m68k_op_muls_16_d(); break;
            case 1491: m68k_op_muls_16_ai(); break;
            case 1492: m68k_op_muls_16_pi(); break;
            case 1493: m68k_op_muls_16_pd(); break;
            case 1494: m68k_op_muls_16_di(); break;
            case 1495: m68k_op_muls_16_ix(); break;
            case 1496: m68k_op_muls_16_aw(); break;
            case 1497: m68k_op_muls_16_al(); break;
            case 1498: m68k_op_muls_16_pcdi(); break;
            case 1499: m68k_op_muls_16_pcix(); break;
            case 1500: m68k_op_muls_16_i(); break;
            case 1501: m68k_op_abcd_8_mm_ax7(); break;
            case 1502: m68k_op_abcd_8_mm_axy7(); break;
            case 1503: m68k_op_add_8_er_d(); break;
            case 1504: m68k_op_add_8_er_ai(); break;
            case 1505: m68k_op_add_8_er_pi(); break;
            case 1506: m68k_op_add_8_er_pi7(); break;
            case 1507: m68k_op_add_8_er_pd(); break;
            case 1508: m68k_op_add_8_er_pd7(); break;
            case 1509: m68k_op_add_8_er_di(); break;
            case 1510: m68k_op_add_8_er_ix(); break;
            case 1511: m68k_op_add_8_er_aw(); break;
            case 1512: m68k_op_add_8_er_al(); break;
            case 1513: m68k_op_add_8_er_pcdi(); break;
            case 1514: m68k_op_add_8_er_pcix(); break;
            case 1515: m68k_op_add_8_er_i(); break;
            case 1516: m68k_op_add_16_er_d(); break;
            case 1517: m68k_op_add_16_er_a(); break;
            case 1518: m68k_op_add_16_er_ai(); break;
            case 1519: m68k_op_add_16_er_pi(); break;
            case 1520: m68k_op_add_16_er_pd(); break;
            case 1521: m68k_op_add_16_er_di(); break;
            case 1522: m68k_op_add_16_er_ix(); break;
            case 1523: m68k_op_add_16_er_aw(); break;
            case 1524: m68k_op_add_16_er_al(); break;
            case 1525: m68k_op_add_16_er_pcdi(); break;
            case 1526: m68k_op_add_16_er_pcix(); break;
            case 1527: m68k_op_add_16_er_i(); break;
            case 1528: m68k_op_add_32_er_d(); break;
            case 1529: m68k_op_add_32_er_a(); break;
            case 1530: m68k_op_add_32_er_ai(); break;
            case 1531: m68k_op_add_32_er_pi(); break;
            case 1532: m68k_op_add_32_er_pd(); break;
            case 1533: m68k_op_add_32_er_di(); break;
            case 1534: m68k_op_add_32_er_ix(); break;
            case 1535: m68k_op_add_32_er_aw(); break;
            case 1536: m68k_op_add_32_er_al(); break;
            case 1537: m68k_op_add_32_er_pcdi(); break;
            case 1538: m68k_op_add_32_er_pcix(); break;
            case 1539: m68k_op_add_32_er_i(); break;
            case 1540: m68k_op_adda_16_d(); break;
            case 1541: m68k_op_adda_16_a(); break;
            case 1542: m68k_op_adda_16_ai(); break;
            case 1543: m68k_op_adda_16_pi(); break;
            case 1544: m68k_op_adda_16_pd(); break;
            case 1545: m68k_op_adda_16_di(); break;
            case 1546: m68k_op_adda_16_ix(); break;
            case 1547: m68k_op_adda_16_aw(); break;
            case 1548: m68k_op_adda_16_al(); break;
            case 1549: m68k_op_adda_16_pcdi(); break;
            case 1550: m68k_op_adda_16_pcix(); break;
            case 1551: m68k_op_adda_16_i(); break;
            case 1552: m68k_op_addx_8_rr(); break;
            case 1553: m68k_op_addx_8_mm(); break;
            case 1554: m68k_op_addx_8_mm_ay7(); break;
            case 1555: m68k_op_add_8_re_ai(); break;
            case 1556: m68k_op_add_8_re_pi(); break;
            case 1557: m68k_op_add_8_re_pi7(); break;
            case 1558: m68k_op_add_8_re_pd(); break;
            case 1559: m68k_op_add_8_re_pd7(); break;
            case 1560: m68k_op_add_8_re_di(); break;
            case 1561: m68k_op_add_8_re_ix(); break;
            case 1562: m68k_op_add_8_re_aw(); break;
            case 1563: m68k_op_add_8_re_al(); break;
            case 1564: m68k_op_addx_16_rr(); break;
            case 1565: m68k_op_addx_16_mm(); break;
            case 1566: m68k_op_add_16_re_ai(); break;
            case 1567: m68k_op_add_16_re_pi(); break;
            case 1568: m68k_op_add_16_re_pd(); break;
            case 1569: m68k_op_add_16_re_di(); break;
            case 1570: m68k_op_add_16_re_ix(); break;
            case 1571: m68k_op_add_16_re_aw(); break;
            case 1572: m68k_op_add_16_re_al(); break;
            case 1573: m68k_op_addx_32_rr(); break;
            case 1574: m68k_op_addx_32_mm(); break;
            case 1575: m68k_op_add_32_re_ai(); break;
            case 1576: m68k_op_add_32_re_pi(); break;
            case 1577: m68k_op_add_32_re_pd(); break;
            case 1578: m68k_op_add_32_re_di(); break;
            case 1579: m68k_op_add_32_re_ix(); break;
            case 1580: m68k_op_add_32_re_aw(); break;
            case 1581: m68k_op_add_32_re_al(); break;
            case 1582: m68k_op_adda_32_d(); break;
            case 1583: m68k_op_adda_32_a(); break;
            case 1584: m68k_op_adda_32_ai(); break;
            case 1585: m68k_op_adda_32_pi(); break;
            case 1586: m68k_op_adda_32_pd(); break;
            case 1587: m68k_op_adda_32_di(); break;
            case 1588: m68k_op_adda_32_ix(); break;
            case 1589: m68k_op_adda_32_aw(); break;
            case 1590: m68k_op_adda_32_al(); break;
            case 1591: m68k_op_adda_32_pcdi(); break;
            case 1592: m68k_op_adda_32_pcix(); break;
            case 1593: m68k_op_adda_32_i(); break;
            case 1594: m68k_op_addx_8_mm_ax7(); break;
            case 1595: m68k_op_addx_8_mm_axy7(); break;
            case 1596: m68k_op_asr_8_s(); break;
            case 1597: m68k_op_lsr_8_s(); break;
            case 1598: m68k_op_roxr_8_s(); break;
            case 1599: m68k_op_ror_8_s(); break;
            case 1600: m68k_op_asr_8_r(); break;
            case 1601: m68k_op_lsr_8_r(); break;
            case 1602: m68k_op_roxr_8_r(); break;
            case 1603: m68k_op_ror_8_r(); break;
            case 1604: m68k_op_asr_16_s(); break;
            case 1605: m68k_op_lsr_16_s(); break;
            case 1606: m68k_op_roxr_16_s(); break;
            case 1607: m68k_op_ror_16_s(); break;
            case 1608: m68k_op_asr_16_r(); break;
            case 1609: m68k_op_lsr_16_r(); break;
            case 1610: m68k_op_roxr_16_r(); break;
            case 1611: m68k_op_ror_16_r(); break;
            case 1612: m68k_op_asr_32_s(); break;
            case 1613: m68k_op_lsr_32_s(); break;
            case 1614: m68k_op_roxr_32_s(); break;
            case 1615: m68k_op_ror_32_s(); break;
            case 1616: m68k_op_asr_32_r(); break;
            case 1617: m68k_op_lsr_32_r(); break;
            case 1618: m68k_op_roxr_32_r(); break;
            case 1619: m68k_op_ror_32_r(); break;
            case 1620: m68k_op_asr_16_ai(); break;
            case 1621: m68k_op_asr_16_pi(); break;
            case 1622: m68k_op_asr_16_pd(); break;
            case 1623: m68k_op_asr_16_di(); break;
            case 1624: m68k_op_asr_16_ix(); break;
            case 1625: m68k_op_asr_16_aw(); break;
            case 1626: m68k_op_asr_16_al(); break;
            case 1627: m68k_op_asl_8_s(); break;
            case 1628: m68k_op_lsl_8_s(); break;
            case 1629: m68k_op_roxl_8_s(); break;
            case 1630: m68k_op_rol_8_s(); break;
            case 1631: m68k_op_asl_8_r(); break;
            case 1632: m68k_op_lsl_8_r(); break;
            case 1633: m68k_op_roxl_8_r(); break;
            case 1634: m68k_op_rol_8_r(); break;
            case 1635: m68k_op_asl_16_s(); break;
            case 1636: m68k_op_lsl_16_s(); break;
            case 1637: m68k_op_roxl_16_s(); break;
            case 1638: m68k_op_rol_16_s(); break;
            case 1639: m68k_op_asl_16_r(); break;
            case 1640: m68k_op_lsl_16_r(); break;
            case 1641: m68k_op_roxl_16_r(); break;
            case 1642: m68k_op_rol_16_r(); break;
            case 1643: m68k_op_asl_32_s(); break;
            case 1644: m68k_op_lsl_32_s(); break;
            case 1645: m68k_op_roxl_32_s(); break;
            case 1646: m68k_op_rol_32_s(); break;
            case 1647: m68k_op_asl_32_r(); break;
            case 1648: m68k_op_lsl_32_r(); break;
            case 1649: m68k_op_roxl_32_r(); break;
            case 1650: m68k_op_rol_32_r(); break;
            case 1651: m68k_op_asl_16_ai(); break;
            case 1652: m68k_op_asl_16_pi(); break;
            case 1653: m68k_op_asl_16_pd(); break;
            case 1654: m68k_op_asl_16_di(); break;
            case 1655: m68k_op_asl_16_ix(); break;
            case 1656: m68k_op_asl_16_aw(); break;
            case 1657: m68k_op_asl_16_al(); break;
            case 1658: m68k_op_lsr_16_ai(); break;
            case 1659: m68k_op_lsr_16_pi(); break;
            case 1660: m68k_op_lsr_16_pd(); break;
            case 1661: m68k_op_lsr_16_di(); break;
            case 1662: m68k_op_lsr_16_ix(); break;
            case 1663: m68k_op_lsr_16_aw(); break;
            case 1664: m68k_op_lsr_16_al(); break;
            case 1665: m68k_op_lsl_16_ai(); break;
            case 1666: m68k_op_lsl_16_pi(); break;
            case 1667: m68k_op_lsl_16_pd(); break;
            case 1668: m68k_op_lsl_16_di(); break;
            case 1669: m68k_op_lsl_16_ix(); break;
            case 1670: m68k_op_lsl_16_aw(); break;
            case 1671: m68k_op_lsl_16_al(); break;
            case 1672: m68k_op_roxr_16_ai(); break;
            case 1673: m68k_op_roxr_16_pi(); break;
            case 1674: m68k_op_roxr_16_pd(); break;
            case 1675: m68k_op_roxr_16_di(); break;
            case 1676: m68k_op_roxr_16_ix(); break;
            case 1677: m68k_op_roxr_16_aw(); break;
            case 1678: m68k_op_roxr_16_al(); break;
            case 1679: m68k_op_roxl_16_ai(); break;
            case 1680: m68k_op_roxl_16_pi(); break;
            case 1681: m68k_op_roxl_16_pd(); break;
            case 1682: m68k_op_roxl_16_di(); break;
            case 1683: m68k_op_roxl_16_ix(); break;
            case 1684: m68k_op_roxl_16_aw(); break;
            case 1685: m68k_op_roxl_16_al(); break;
            case 1686: m68k_op_ror_16_ai(); break;
            case 1687: m68k_op_ror_16_pi(); break;
            case 1688: m68k_op_ror_16_pd(); break;
            case 1689: m68k_op_ror_16_di(); break;
            case 1690: m68k_op_ror_16_ix(); break;
            case 1691: m68k_op_ror_16_aw(); break;
            case 1692: m68k_op_ror_16_al(); break;
            case 1693: m68k_op_rol_16_ai(); break;
            case 1694: m68k_op_rol_16_pi(); break;
            case 1695: m68k_op_rol_16_pd(); break;
            case 1696: m68k_op_rol_16_di(); break;
            case 1697: m68k_op_rol_16_ix(); break;
            case 1698: m68k_op_rol_16_aw(); break;
            case 1699: m68k_op_rol_16_al(); break;
            case 1700: m68k_op_1111(); break;
        }
    }
    private static ReadOnlySpan<ushort> InstructionMap =>
    [
        0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 4,
        5, 5, 5, 5, 5, 5, 5, 6, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 9, 10, 1, 1, 11, 1, 1, 1,
        12, 12, 12, 12, 12, 12, 12, 12, 1, 1, 1, 1, 1, 1, 1, 1, 13, 13, 13, 13, 13, 13, 13, 13, 14, 14, 14, 14, 14, 14, 14, 14,
        15, 15, 15, 15, 15, 15, 15, 15, 16, 16, 16, 16, 16, 16, 16, 16, 17, 17, 17, 17, 17, 17, 17, 17, 18, 19, 1, 1, 20, 1, 1, 1,
        21, 21, 21, 21, 21, 21, 21, 21, 1, 1, 1, 1, 1, 1, 1, 1, 22, 22, 22, 22, 22, 22, 22, 22, 23, 23, 23, 23, 23, 23, 23, 23,
        24, 24, 24, 24, 24, 24, 24, 24, 25, 25, 25, 25, 25, 25, 25, 25, 26, 26, 26, 26, 26, 26, 26, 26, 27, 28, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        29, 29, 29, 29, 29, 29, 29, 29, 30, 30, 30, 30, 30, 30, 30, 30, 31, 31, 31, 31, 31, 31, 31, 31, 32, 32, 32, 32, 32, 32, 32, 33,
        34, 34, 34, 34, 34, 34, 34, 35, 36, 36, 36, 36, 36, 36, 36, 36, 37, 37, 37, 37, 37, 37, 37, 37, 38, 39, 40, 41, 42, 1, 1, 1,
        43, 43, 43, 43, 43, 43, 43, 43, 44, 44, 44, 44, 44, 44, 44, 44, 45, 45, 45, 45, 45, 45, 45, 45, 46, 46, 46, 46, 46, 46, 46, 47,
        48, 48, 48, 48, 48, 48, 48, 49, 50, 50, 50, 50, 50, 50, 50, 50, 51, 51, 51, 51, 51, 51, 51, 51, 52, 53, 1, 1, 1, 1, 1, 1,
        54, 54, 54, 54, 54, 54, 54, 54, 55, 55, 55, 55, 55, 55, 55, 55, 56, 56, 56, 56, 56, 56, 56, 56, 57, 57, 57, 57, 57, 57, 57, 58,
        59, 59, 59, 59, 59, 59, 59, 60, 61, 61, 61, 61, 61, 61, 61, 61, 62, 62, 62, 62, 62, 62, 62, 62, 63, 64, 1, 1, 1, 1, 1, 1,
        65, 65, 65, 65, 65, 65, 65, 65, 66, 66, 66, 66, 66, 66, 66, 66, 67, 67, 67, 67, 67, 67, 67, 67, 68, 68, 68, 68, 68, 68, 68, 69,
        70, 70, 70, 70, 70, 70, 70, 71, 72, 72, 72, 72, 72, 72, 72, 72, 73, 73, 73, 73, 73, 73, 73, 73, 74, 75, 1, 1, 1, 1, 1, 1,
        76, 76, 76, 76, 76, 76, 76, 76, 1, 1, 1, 1, 1, 1, 1, 1, 77, 77, 77, 77, 77, 77, 77, 77, 78, 78, 78, 78, 78, 78, 78, 79,
        80, 80, 80, 80, 80, 80, 80, 81, 82, 82, 82, 82, 82, 82, 82, 82, 83, 83, 83, 83, 83, 83, 83, 83, 84, 85, 1, 1, 86, 1, 1, 1,
        87, 87, 87, 87, 87, 87, 87, 87, 1, 1, 1, 1, 1, 1, 1, 1, 88, 88, 88, 88, 88, 88, 88, 88, 89, 89, 89, 89, 89, 89, 89, 89,
        90, 90, 90, 90, 90, 90, 90, 90, 91, 91, 91, 91, 91, 91, 91, 91, 92, 92, 92, 92, 92, 92, 92, 92, 93, 94, 1, 1, 95, 1, 1, 1,
        96, 96, 96, 96, 96, 96, 96, 96, 1, 1, 1, 1, 1, 1, 1, 1, 97, 97, 97, 97, 97, 97, 97, 97, 98, 98, 98, 98, 98, 98, 98, 98,
        99, 99, 99, 99, 99, 99, 99, 99, 100, 100, 100, 100, 100, 100, 100, 100, 101, 101, 101, 101, 101, 101, 101, 101, 102, 103, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        29, 29, 29, 29, 29, 29, 29, 29, 30, 30, 30, 30, 30, 30, 30, 30, 31, 31, 31, 31, 31, 31, 31, 31, 32, 32, 32, 32, 32, 32, 32, 33,
        34, 34, 34, 34, 34, 34, 34, 35, 36, 36, 36, 36, 36, 36, 36, 36, 37, 37, 37, 37, 37, 37, 37, 37, 38, 39, 40, 41, 42, 1, 1, 1,
        43, 43, 43, 43, 43, 43, 43, 43, 44, 44, 44, 44, 44, 44, 44, 44, 45, 45, 45, 45, 45, 45, 45, 45, 46, 46, 46, 46, 46, 46, 46, 47,
        48, 48, 48, 48, 48, 48, 48, 49, 50, 50, 50, 50, 50, 50, 50, 50, 51, 51, 51, 51, 51, 51, 51, 51, 52, 53, 1, 1, 1, 1, 1, 1,
        54, 54, 54, 54, 54, 54, 54, 54, 55, 55, 55, 55, 55, 55, 55, 55, 56, 56, 56, 56, 56, 56, 56, 56, 57, 57, 57, 57, 57, 57, 57, 58,
        59, 59, 59, 59, 59, 59, 59, 60, 61, 61, 61, 61, 61, 61, 61, 61, 62, 62, 62, 62, 62, 62, 62, 62, 63, 64, 1, 1, 1, 1, 1, 1,
        65, 65, 65, 65, 65, 65, 65, 65, 66, 66, 66, 66, 66, 66, 66, 66, 67, 67, 67, 67, 67, 67, 67, 67, 68, 68, 68, 68, 68, 68, 68, 69,
        70, 70, 70, 70, 70, 70, 70, 71, 72, 72, 72, 72, 72, 72, 72, 72, 73, 73, 73, 73, 73, 73, 73, 73, 74, 75, 1, 1, 1, 1, 1, 1,
        104, 104, 104, 104, 104, 104, 104, 104, 1, 1, 1, 1, 1, 1, 1, 1, 105, 105, 105, 105, 105, 105, 105, 105, 106, 106, 106, 106, 106, 106, 106, 107,
        108, 108, 108, 108, 108, 108, 108, 109, 110, 110, 110, 110, 110, 110, 110, 110, 111, 111, 111, 111, 111, 111, 111, 111, 112, 113, 1, 1, 1, 1, 1, 1,
        114, 114, 114, 114, 114, 114, 114, 114, 1, 1, 1, 1, 1, 1, 1, 1, 115, 115, 115, 115, 115, 115, 115, 115, 116, 116, 116, 116, 116, 116, 116, 116,
        117, 117, 117, 117, 117, 117, 117, 117, 118, 118, 118, 118, 118, 118, 118, 118, 119, 119, 119, 119, 119, 119, 119, 119, 120, 121, 1, 1, 1, 1, 1, 1,
        122, 122, 122, 122, 122, 122, 122, 122, 1, 1, 1, 1, 1, 1, 1, 1, 123, 123, 123, 123, 123, 123, 123, 123, 124, 124, 124, 124, 124, 124, 124, 124,
        125, 125, 125, 125, 125, 125, 125, 125, 126, 126, 126, 126, 126, 126, 126, 126, 127, 127, 127, 127, 127, 127, 127, 127, 128, 129, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        29, 29, 29, 29, 29, 29, 29, 29, 30, 30, 30, 30, 30, 30, 30, 30, 31, 31, 31, 31, 31, 31, 31, 31, 32, 32, 32, 32, 32, 32, 32, 33,
        34, 34, 34, 34, 34, 34, 34, 35, 36, 36, 36, 36, 36, 36, 36, 36, 37, 37, 37, 37, 37, 37, 37, 37, 38, 39, 40, 41, 42, 1, 1, 1,
        43, 43, 43, 43, 43, 43, 43, 43, 44, 44, 44, 44, 44, 44, 44, 44, 45, 45, 45, 45, 45, 45, 45, 45, 46, 46, 46, 46, 46, 46, 46, 47,
        48, 48, 48, 48, 48, 48, 48, 49, 50, 50, 50, 50, 50, 50, 50, 50, 51, 51, 51, 51, 51, 51, 51, 51, 52, 53, 1, 1, 1, 1, 1, 1,
        54, 54, 54, 54, 54, 54, 54, 54, 55, 55, 55, 55, 55, 55, 55, 55, 56, 56, 56, 56, 56, 56, 56, 56, 57, 57, 57, 57, 57, 57, 57, 58,
        59, 59, 59, 59, 59, 59, 59, 60, 61, 61, 61, 61, 61, 61, 61, 61, 62, 62, 62, 62, 62, 62, 62, 62, 63, 64, 1, 1, 1, 1, 1, 1,
        65, 65, 65, 65, 65, 65, 65, 65, 66, 66, 66, 66, 66, 66, 66, 66, 67, 67, 67, 67, 67, 67, 67, 67, 68, 68, 68, 68, 68, 68, 68, 69,
        70, 70, 70, 70, 70, 70, 70, 71, 72, 72, 72, 72, 72, 72, 72, 72, 73, 73, 73, 73, 73, 73, 73, 73, 74, 75, 1, 1, 1, 1, 1, 1,
        130, 130, 130, 130, 130, 130, 130, 130, 1, 1, 1, 1, 1, 1, 1, 1, 131, 131, 131, 131, 131, 131, 131, 131, 132, 132, 132, 132, 132, 132, 132, 133,
        134, 134, 134, 134, 134, 134, 134, 135, 136, 136, 136, 136, 136, 136, 136, 136, 137, 137, 137, 137, 137, 137, 137, 137, 138, 139, 1, 1, 1, 1, 1, 1,
        140, 140, 140, 140, 140, 140, 140, 140, 1, 1, 1, 1, 1, 1, 1, 1, 141, 141, 141, 141, 141, 141, 141, 141, 142, 142, 142, 142, 142, 142, 142, 142,
        143, 143, 143, 143, 143, 143, 143, 143, 144, 144, 144, 144, 144, 144, 144, 144, 145, 145, 145, 145, 145, 145, 145, 145, 146, 147, 1, 1, 1, 1, 1, 1,
        148, 148, 148, 148, 148, 148, 148, 148, 1, 1, 1, 1, 1, 1, 1, 1, 149, 149, 149, 149, 149, 149, 149, 149, 150, 150, 150, 150, 150, 150, 150, 150,
        151, 151, 151, 151, 151, 151, 151, 151, 152, 152, 152, 152, 152, 152, 152, 152, 153, 153, 153, 153, 153, 153, 153, 153, 154, 155, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        29, 29, 29, 29, 29, 29, 29, 29, 30, 30, 30, 30, 30, 30, 30, 30, 31, 31, 31, 31, 31, 31, 31, 31, 32, 32, 32, 32, 32, 32, 32, 33,
        34, 34, 34, 34, 34, 34, 34, 35, 36, 36, 36, 36, 36, 36, 36, 36, 37, 37, 37, 37, 37, 37, 37, 37, 38, 39, 40, 41, 42, 1, 1, 1,
        43, 43, 43, 43, 43, 43, 43, 43, 44, 44, 44, 44, 44, 44, 44, 44, 45, 45, 45, 45, 45, 45, 45, 45, 46, 46, 46, 46, 46, 46, 46, 47,
        48, 48, 48, 48, 48, 48, 48, 49, 50, 50, 50, 50, 50, 50, 50, 50, 51, 51, 51, 51, 51, 51, 51, 51, 52, 53, 1, 1, 1, 1, 1, 1,
        54, 54, 54, 54, 54, 54, 54, 54, 55, 55, 55, 55, 55, 55, 55, 55, 56, 56, 56, 56, 56, 56, 56, 56, 57, 57, 57, 57, 57, 57, 57, 58,
        59, 59, 59, 59, 59, 59, 59, 60, 61, 61, 61, 61, 61, 61, 61, 61, 62, 62, 62, 62, 62, 62, 62, 62, 63, 64, 1, 1, 1, 1, 1, 1,
        65, 65, 65, 65, 65, 65, 65, 65, 66, 66, 66, 66, 66, 66, 66, 66, 67, 67, 67, 67, 67, 67, 67, 67, 68, 68, 68, 68, 68, 68, 68, 69,
        70, 70, 70, 70, 70, 70, 70, 71, 72, 72, 72, 72, 72, 72, 72, 72, 73, 73, 73, 73, 73, 73, 73, 73, 74, 75, 1, 1, 1, 1, 1, 1,
        156, 156, 156, 156, 156, 156, 156, 156, 1, 1, 1, 1, 1, 1, 1, 1, 157, 157, 157, 157, 157, 157, 157, 157, 158, 158, 158, 158, 158, 158, 158, 159,
        160, 160, 160, 160, 160, 160, 160, 161, 162, 162, 162, 162, 162, 162, 162, 162, 163, 163, 163, 163, 163, 163, 163, 163, 164, 165, 166, 167, 1, 1, 1, 1,
        168, 168, 168, 168, 168, 168, 168, 168, 1, 1, 1, 1, 1, 1, 1, 1, 169, 169, 169, 169, 169, 169, 169, 169, 170, 170, 170, 170, 170, 170, 170, 171,
        172, 172, 172, 172, 172, 172, 172, 173, 174, 174, 174, 174, 174, 174, 174, 174, 175, 175, 175, 175, 175, 175, 175, 175, 176, 177, 1, 1, 1, 1, 1, 1,
        178, 178, 178, 178, 178, 178, 178, 178, 1, 1, 1, 1, 1, 1, 1, 1, 179, 179, 179, 179, 179, 179, 179, 179, 180, 180, 180, 180, 180, 180, 180, 181,
        182, 182, 182, 182, 182, 182, 182, 183, 184, 184, 184, 184, 184, 184, 184, 184, 185, 185, 185, 185, 185, 185, 185, 185, 186, 187, 1, 1, 1, 1, 1, 1,
        188, 188, 188, 188, 188, 188, 188, 188, 1, 1, 1, 1, 1, 1, 1, 1, 189, 189, 189, 189, 189, 189, 189, 189, 190, 190, 190, 190, 190, 190, 190, 191,
        192, 192, 192, 192, 192, 192, 192, 193, 194, 194, 194, 194, 194, 194, 194, 194, 195, 195, 195, 195, 195, 195, 195, 195, 196, 197, 1, 1, 1, 1, 1, 1,
        29, 29, 29, 29, 29, 29, 29, 29, 30, 30, 30, 30, 30, 30, 30, 30, 31, 31, 31, 31, 31, 31, 31, 31, 32, 32, 32, 32, 32, 32, 32, 33,
        34, 34, 34, 34, 34, 34, 34, 35, 36, 36, 36, 36, 36, 36, 36, 36, 37, 37, 37, 37, 37, 37, 37, 37, 38, 39, 40, 41, 42, 1, 1, 1,
        43, 43, 43, 43, 43, 43, 43, 43, 44, 44, 44, 44, 44, 44, 44, 44, 45, 45, 45, 45, 45, 45, 45, 45, 46, 46, 46, 46, 46, 46, 46, 47,
        48, 48, 48, 48, 48, 48, 48, 49, 50, 50, 50, 50, 50, 50, 50, 50, 51, 51, 51, 51, 51, 51, 51, 51, 52, 53, 1, 1, 1, 1, 1, 1,
        54, 54, 54, 54, 54, 54, 54, 54, 55, 55, 55, 55, 55, 55, 55, 55, 56, 56, 56, 56, 56, 56, 56, 56, 57, 57, 57, 57, 57, 57, 57, 58,
        59, 59, 59, 59, 59, 59, 59, 60, 61, 61, 61, 61, 61, 61, 61, 61, 62, 62, 62, 62, 62, 62, 62, 62, 63, 64, 1, 1, 1, 1, 1, 1,
        65, 65, 65, 65, 65, 65, 65, 65, 66, 66, 66, 66, 66, 66, 66, 66, 67, 67, 67, 67, 67, 67, 67, 67, 68, 68, 68, 68, 68, 68, 68, 69,
        70, 70, 70, 70, 70, 70, 70, 71, 72, 72, 72, 72, 72, 72, 72, 72, 73, 73, 73, 73, 73, 73, 73, 73, 74, 75, 1, 1, 1, 1, 1, 1,
        198, 198, 198, 198, 198, 198, 198, 198, 1, 1, 1, 1, 1, 1, 1, 1, 199, 199, 199, 199, 199, 199, 199, 199, 200, 200, 200, 200, 200, 200, 200, 201,
        202, 202, 202, 202, 202, 202, 202, 203, 204, 204, 204, 204, 204, 204, 204, 204, 205, 205, 205, 205, 205, 205, 205, 205, 206, 207, 1, 1, 208, 1, 1, 1,
        209, 209, 209, 209, 209, 209, 209, 209, 1, 1, 1, 1, 1, 1, 1, 1, 210, 210, 210, 210, 210, 210, 210, 210, 211, 211, 211, 211, 211, 211, 211, 211,
        212, 212, 212, 212, 212, 212, 212, 212, 213, 213, 213, 213, 213, 213, 213, 213, 214, 214, 214, 214, 214, 214, 214, 214, 215, 216, 1, 1, 217, 1, 1, 1,
        218, 218, 218, 218, 218, 218, 218, 218, 1, 1, 1, 1, 1, 1, 1, 1, 219, 219, 219, 219, 219, 219, 219, 219, 220, 220, 220, 220, 220, 220, 220, 220,
        221, 221, 221, 221, 221, 221, 221, 221, 222, 222, 222, 222, 222, 222, 222, 222, 223, 223, 223, 223, 223, 223, 223, 223, 224, 225, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        29, 29, 29, 29, 29, 29, 29, 29, 30, 30, 30, 30, 30, 30, 30, 30, 31, 31, 31, 31, 31, 31, 31, 31, 32, 32, 32, 32, 32, 32, 32, 33,
        34, 34, 34, 34, 34, 34, 34, 35, 36, 36, 36, 36, 36, 36, 36, 36, 37, 37, 37, 37, 37, 37, 37, 37, 38, 39, 40, 41, 42, 1, 1, 1,
        43, 43, 43, 43, 43, 43, 43, 43, 44, 44, 44, 44, 44, 44, 44, 44, 45, 45, 45, 45, 45, 45, 45, 45, 46, 46, 46, 46, 46, 46, 46, 47,
        48, 48, 48, 48, 48, 48, 48, 49, 50, 50, 50, 50, 50, 50, 50, 50, 51, 51, 51, 51, 51, 51, 51, 51, 52, 53, 1, 1, 1, 1, 1, 1,
        54, 54, 54, 54, 54, 54, 54, 54, 55, 55, 55, 55, 55, 55, 55, 55, 56, 56, 56, 56, 56, 56, 56, 56, 57, 57, 57, 57, 57, 57, 57, 58,
        59, 59, 59, 59, 59, 59, 59, 60, 61, 61, 61, 61, 61, 61, 61, 61, 62, 62, 62, 62, 62, 62, 62, 62, 63, 64, 1, 1, 1, 1, 1, 1,
        65, 65, 65, 65, 65, 65, 65, 65, 66, 66, 66, 66, 66, 66, 66, 66, 67, 67, 67, 67, 67, 67, 67, 67, 68, 68, 68, 68, 68, 68, 68, 69,
        70, 70, 70, 70, 70, 70, 70, 71, 72, 72, 72, 72, 72, 72, 72, 72, 73, 73, 73, 73, 73, 73, 73, 73, 74, 75, 1, 1, 1, 1, 1, 1,
        226, 226, 226, 226, 226, 226, 226, 226, 1, 1, 1, 1, 1, 1, 1, 1, 227, 227, 227, 227, 227, 227, 227, 227, 228, 228, 228, 228, 228, 228, 228, 229,
        230, 230, 230, 230, 230, 230, 230, 231, 232, 232, 232, 232, 232, 232, 232, 232, 233, 233, 233, 233, 233, 233, 233, 233, 234, 235, 1, 1, 1, 1, 1, 1,
        236, 236, 236, 236, 236, 236, 236, 236, 1, 1, 1, 1, 1, 1, 1, 1, 237, 237, 237, 237, 237, 237, 237, 237, 238, 238, 238, 238, 238, 238, 238, 238,
        239, 239, 239, 239, 239, 239, 239, 239, 240, 240, 240, 240, 240, 240, 240, 240, 241, 241, 241, 241, 241, 241, 241, 241, 242, 243, 1, 1, 1, 1, 1, 1,
        244, 244, 244, 244, 244, 244, 244, 244, 1, 1, 1, 1, 1, 1, 1, 1, 245, 245, 245, 245, 245, 245, 245, 245, 246, 246, 246, 246, 246, 246, 246, 246,
        247, 247, 247, 247, 247, 247, 247, 247, 248, 248, 248, 248, 248, 248, 248, 248, 249, 249, 249, 249, 249, 249, 249, 249, 250, 251, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        29, 29, 29, 29, 29, 29, 29, 29, 30, 30, 30, 30, 30, 30, 30, 30, 31, 31, 31, 31, 31, 31, 31, 31, 32, 32, 32, 32, 32, 32, 32, 33,
        34, 34, 34, 34, 34, 34, 34, 35, 36, 36, 36, 36, 36, 36, 36, 36, 37, 37, 37, 37, 37, 37, 37, 37, 38, 39, 40, 41, 42, 1, 1, 1,
        43, 43, 43, 43, 43, 43, 43, 43, 44, 44, 44, 44, 44, 44, 44, 44, 45, 45, 45, 45, 45, 45, 45, 45, 46, 46, 46, 46, 46, 46, 46, 47,
        48, 48, 48, 48, 48, 48, 48, 49, 50, 50, 50, 50, 50, 50, 50, 50, 51, 51, 51, 51, 51, 51, 51, 51, 52, 53, 1, 1, 1, 1, 1, 1,
        54, 54, 54, 54, 54, 54, 54, 54, 55, 55, 55, 55, 55, 55, 55, 55, 56, 56, 56, 56, 56, 56, 56, 56, 57, 57, 57, 57, 57, 57, 57, 58,
        59, 59, 59, 59, 59, 59, 59, 60, 61, 61, 61, 61, 61, 61, 61, 61, 62, 62, 62, 62, 62, 62, 62, 62, 63, 64, 1, 1, 1, 1, 1, 1,
        65, 65, 65, 65, 65, 65, 65, 65, 66, 66, 66, 66, 66, 66, 66, 66, 67, 67, 67, 67, 67, 67, 67, 67, 68, 68, 68, 68, 68, 68, 68, 69,
        70, 70, 70, 70, 70, 70, 70, 71, 72, 72, 72, 72, 72, 72, 72, 72, 73, 73, 73, 73, 73, 73, 73, 73, 74, 75, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        29, 29, 29, 29, 29, 29, 29, 29, 30, 30, 30, 30, 30, 30, 30, 30, 31, 31, 31, 31, 31, 31, 31, 31, 32, 32, 32, 32, 32, 32, 32, 33,
        34, 34, 34, 34, 34, 34, 34, 35, 36, 36, 36, 36, 36, 36, 36, 36, 37, 37, 37, 37, 37, 37, 37, 37, 38, 39, 40, 41, 42, 1, 1, 1,
        43, 43, 43, 43, 43, 43, 43, 43, 44, 44, 44, 44, 44, 44, 44, 44, 45, 45, 45, 45, 45, 45, 45, 45, 46, 46, 46, 46, 46, 46, 46, 47,
        48, 48, 48, 48, 48, 48, 48, 49, 50, 50, 50, 50, 50, 50, 50, 50, 51, 51, 51, 51, 51, 51, 51, 51, 52, 53, 1, 1, 1, 1, 1, 1,
        54, 54, 54, 54, 54, 54, 54, 54, 55, 55, 55, 55, 55, 55, 55, 55, 56, 56, 56, 56, 56, 56, 56, 56, 57, 57, 57, 57, 57, 57, 57, 58,
        59, 59, 59, 59, 59, 59, 59, 60, 61, 61, 61, 61, 61, 61, 61, 61, 62, 62, 62, 62, 62, 62, 62, 62, 63, 64, 1, 1, 1, 1, 1, 1,
        65, 65, 65, 65, 65, 65, 65, 65, 66, 66, 66, 66, 66, 66, 66, 66, 67, 67, 67, 67, 67, 67, 67, 67, 68, 68, 68, 68, 68, 68, 68, 69,
        70, 70, 70, 70, 70, 70, 70, 71, 72, 72, 72, 72, 72, 72, 72, 72, 73, 73, 73, 73, 73, 73, 73, 73, 74, 75, 1, 1, 1, 1, 1, 1,
        252, 252, 252, 252, 252, 252, 252, 252, 1, 1, 1, 1, 1, 1, 1, 1, 253, 253, 253, 253, 253, 253, 253, 253, 254, 254, 254, 254, 254, 254, 254, 255,
        256, 256, 256, 256, 256, 256, 256, 257, 258, 258, 258, 258, 258, 258, 258, 258, 259, 259, 259, 259, 259, 259, 259, 259, 260, 261, 262, 263, 264, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        265, 265, 265, 265, 265, 265, 265, 265, 1, 1, 1, 1, 1, 1, 1, 1, 266, 266, 266, 266, 266, 266, 266, 266, 267, 267, 267, 267, 267, 267, 267, 268,
        269, 269, 269, 269, 269, 269, 269, 270, 271, 271, 271, 271, 271, 271, 271, 271, 272, 272, 272, 272, 272, 272, 272, 272, 273, 274, 275, 276, 277, 1, 1, 1,
        278, 278, 278, 278, 278, 278, 278, 278, 1, 1, 1, 1, 1, 1, 1, 1, 279, 279, 279, 279, 279, 279, 279, 279, 280, 280, 280, 280, 280, 280, 280, 281,
        282, 282, 282, 282, 282, 282, 282, 283, 284, 284, 284, 284, 284, 284, 284, 284, 285, 285, 285, 285, 285, 285, 285, 285, 286, 287, 288, 289, 290, 1, 1, 1,
        291, 291, 291, 291, 291, 291, 291, 291, 1, 1, 1, 1, 1, 1, 1, 1, 292, 292, 292, 292, 292, 292, 292, 292, 293, 293, 293, 293, 293, 293, 293, 294,
        295, 295, 295, 295, 295, 295, 295, 296, 297, 297, 297, 297, 297, 297, 297, 297, 298, 298, 298, 298, 298, 298, 298, 298, 299, 300, 301, 302, 303, 1, 1, 1,
        304, 304, 304, 304, 304, 304, 304, 304, 1, 1, 1, 1, 1, 1, 1, 1, 305, 305, 305, 305, 305, 305, 305, 305, 306, 306, 306, 306, 306, 306, 306, 307,
        308, 308, 308, 308, 308, 308, 308, 309, 310, 310, 310, 310, 310, 310, 310, 310, 311, 311, 311, 311, 311, 311, 311, 311, 312, 313, 314, 315, 316, 1, 1, 1,
        317, 317, 317, 317, 317, 317, 317, 317, 1, 1, 1, 1, 1, 1, 1, 1, 318, 318, 318, 318, 318, 318, 318, 318, 319, 319, 319, 319, 319, 319, 319, 320,
        321, 321, 321, 321, 321, 321, 321, 322, 323, 323, 323, 323, 323, 323, 323, 323, 324, 324, 324, 324, 324, 324, 324, 324, 325, 326, 327, 328, 329, 1, 1, 1,
        330, 330, 330, 330, 330, 330, 330, 330, 1, 1, 1, 1, 1, 1, 1, 1, 331, 331, 331, 331, 331, 331, 331, 331, 332, 332, 332, 332, 332, 332, 332, 333,
        334, 334, 334, 334, 334, 334, 334, 335, 336, 336, 336, 336, 336, 336, 336, 336, 337, 337, 337, 337, 337, 337, 337, 337, 338, 339, 340, 341, 342, 1, 1, 1,
        252, 252, 252, 252, 252, 252, 252, 252, 1, 1, 1, 1, 1, 1, 1, 1, 253, 253, 253, 253, 253, 253, 253, 253, 254, 254, 254, 254, 254, 254, 254, 255,
        256, 256, 256, 256, 256, 256, 256, 257, 258, 258, 258, 258, 258, 258, 258, 258, 259, 259, 259, 259, 259, 259, 259, 259, 260, 261, 262, 263, 264, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        265, 265, 265, 265, 265, 265, 265, 265, 1, 1, 1, 1, 1, 1, 1, 1, 266, 266, 266, 266, 266, 266, 266, 266, 267, 267, 267, 267, 267, 267, 267, 268,
        269, 269, 269, 269, 269, 269, 269, 270, 271, 271, 271, 271, 271, 271, 271, 271, 272, 272, 272, 272, 272, 272, 272, 272, 273, 274, 275, 276, 277, 1, 1, 1,
        278, 278, 278, 278, 278, 278, 278, 278, 1, 1, 1, 1, 1, 1, 1, 1, 279, 279, 279, 279, 279, 279, 279, 279, 280, 280, 280, 280, 280, 280, 280, 281,
        282, 282, 282, 282, 282, 282, 282, 283, 284, 284, 284, 284, 284, 284, 284, 284, 285, 285, 285, 285, 285, 285, 285, 285, 286, 287, 288, 289, 290, 1, 1, 1,
        291, 291, 291, 291, 291, 291, 291, 291, 1, 1, 1, 1, 1, 1, 1, 1, 292, 292, 292, 292, 292, 292, 292, 292, 293, 293, 293, 293, 293, 293, 293, 294,
        295, 295, 295, 295, 295, 295, 295, 296, 297, 297, 297, 297, 297, 297, 297, 297, 298, 298, 298, 298, 298, 298, 298, 298, 299, 300, 301, 302, 303, 1, 1, 1,
        304, 304, 304, 304, 304, 304, 304, 304, 1, 1, 1, 1, 1, 1, 1, 1, 305, 305, 305, 305, 305, 305, 305, 305, 306, 306, 306, 306, 306, 306, 306, 307,
        308, 308, 308, 308, 308, 308, 308, 309, 310, 310, 310, 310, 310, 310, 310, 310, 311, 311, 311, 311, 311, 311, 311, 311, 312, 313, 314, 315, 316, 1, 1, 1,
        317, 317, 317, 317, 317, 317, 317, 317, 1, 1, 1, 1, 1, 1, 1, 1, 318, 318, 318, 318, 318, 318, 318, 318, 319, 319, 319, 319, 319, 319, 319, 320,
        321, 321, 321, 321, 321, 321, 321, 322, 323, 323, 323, 323, 323, 323, 323, 323, 324, 324, 324, 324, 324, 324, 324, 324, 325, 326, 327, 328, 329, 1, 1, 1,
        343, 343, 343, 343, 343, 343, 343, 343, 1, 1, 1, 1, 1, 1, 1, 1, 344, 344, 344, 344, 344, 344, 344, 344, 345, 345, 345, 345, 345, 345, 345, 346,
        347, 347, 347, 347, 347, 347, 347, 348, 349, 349, 349, 349, 349, 349, 349, 349, 350, 350, 350, 350, 350, 350, 350, 350, 351, 352, 353, 354, 355, 1, 1, 1,
        252, 252, 252, 252, 252, 252, 252, 252, 1, 1, 1, 1, 1, 1, 1, 1, 253, 253, 253, 253, 253, 253, 253, 253, 254, 254, 254, 254, 254, 254, 254, 255,
        256, 256, 256, 256, 256, 256, 256, 257, 258, 258, 258, 258, 258, 258, 258, 258, 259, 259, 259, 259, 259, 259, 259, 259, 260, 261, 262, 263, 264, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        265, 265, 265, 265, 265, 265, 265, 265, 1, 1, 1, 1, 1, 1, 1, 1, 266, 266, 266, 266, 266, 266, 266, 266, 267, 267, 267, 267, 267, 267, 267, 268,
        269, 269, 269, 269, 269, 269, 269, 270, 271, 271, 271, 271, 271, 271, 271, 271, 272, 272, 272, 272, 272, 272, 272, 272, 273, 274, 275, 276, 277, 1, 1, 1,
        278, 278, 278, 278, 278, 278, 278, 278, 1, 1, 1, 1, 1, 1, 1, 1, 279, 279, 279, 279, 279, 279, 279, 279, 280, 280, 280, 280, 280, 280, 280, 281,
        282, 282, 282, 282, 282, 282, 282, 283, 284, 284, 284, 284, 284, 284, 284, 284, 285, 285, 285, 285, 285, 285, 285, 285, 286, 287, 288, 289, 290, 1, 1, 1,
        291, 291, 291, 291, 291, 291, 291, 291, 1, 1, 1, 1, 1, 1, 1, 1, 292, 292, 292, 292, 292, 292, 292, 292, 293, 293, 293, 293, 293, 293, 293, 294,
        295, 295, 295, 295, 295, 295, 295, 296, 297, 297, 297, 297, 297, 297, 297, 297, 298, 298, 298, 298, 298, 298, 298, 298, 299, 300, 301, 302, 303, 1, 1, 1,
        304, 304, 304, 304, 304, 304, 304, 304, 1, 1, 1, 1, 1, 1, 1, 1, 305, 305, 305, 305, 305, 305, 305, 305, 306, 306, 306, 306, 306, 306, 306, 307,
        308, 308, 308, 308, 308, 308, 308, 309, 310, 310, 310, 310, 310, 310, 310, 310, 311, 311, 311, 311, 311, 311, 311, 311, 312, 313, 314, 315, 316, 1, 1, 1,
        317, 317, 317, 317, 317, 317, 317, 317, 1, 1, 1, 1, 1, 1, 1, 1, 318, 318, 318, 318, 318, 318, 318, 318, 319, 319, 319, 319, 319, 319, 319, 320,
        321, 321, 321, 321, 321, 321, 321, 322, 323, 323, 323, 323, 323, 323, 323, 323, 324, 324, 324, 324, 324, 324, 324, 324, 325, 326, 327, 328, 329, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        252, 252, 252, 252, 252, 252, 252, 252, 1, 1, 1, 1, 1, 1, 1, 1, 253, 253, 253, 253, 253, 253, 253, 253, 254, 254, 254, 254, 254, 254, 254, 255,
        256, 256, 256, 256, 256, 256, 256, 257, 258, 258, 258, 258, 258, 258, 258, 258, 259, 259, 259, 259, 259, 259, 259, 259, 260, 261, 262, 263, 264, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        265, 265, 265, 265, 265, 265, 265, 265, 1, 1, 1, 1, 1, 1, 1, 1, 266, 266, 266, 266, 266, 266, 266, 266, 267, 267, 267, 267, 267, 267, 267, 268,
        269, 269, 269, 269, 269, 269, 269, 270, 271, 271, 271, 271, 271, 271, 271, 271, 272, 272, 272, 272, 272, 272, 272, 272, 273, 274, 275, 276, 277, 1, 1, 1,
        278, 278, 278, 278, 278, 278, 278, 278, 1, 1, 1, 1, 1, 1, 1, 1, 279, 279, 279, 279, 279, 279, 279, 279, 280, 280, 280, 280, 280, 280, 280, 281,
        282, 282, 282, 282, 282, 282, 282, 283, 284, 284, 284, 284, 284, 284, 284, 284, 285, 285, 285, 285, 285, 285, 285, 285, 286, 287, 288, 289, 290, 1, 1, 1,
        291, 291, 291, 291, 291, 291, 291, 291, 1, 1, 1, 1, 1, 1, 1, 1, 292, 292, 292, 292, 292, 292, 292, 292, 293, 293, 293, 293, 293, 293, 293, 294,
        295, 295, 295, 295, 295, 295, 295, 296, 297, 297, 297, 297, 297, 297, 297, 297, 298, 298, 298, 298, 298, 298, 298, 298, 299, 300, 301, 302, 303, 1, 1, 1,
        304, 304, 304, 304, 304, 304, 304, 304, 1, 1, 1, 1, 1, 1, 1, 1, 305, 305, 305, 305, 305, 305, 305, 305, 306, 306, 306, 306, 306, 306, 306, 307,
        308, 308, 308, 308, 308, 308, 308, 309, 310, 310, 310, 310, 310, 310, 310, 310, 311, 311, 311, 311, 311, 311, 311, 311, 312, 313, 314, 315, 316, 1, 1, 1,
        317, 317, 317, 317, 317, 317, 317, 317, 1, 1, 1, 1, 1, 1, 1, 1, 318, 318, 318, 318, 318, 318, 318, 318, 319, 319, 319, 319, 319, 319, 319, 320,
        321, 321, 321, 321, 321, 321, 321, 322, 323, 323, 323, 323, 323, 323, 323, 323, 324, 324, 324, 324, 324, 324, 324, 324, 325, 326, 327, 328, 329, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        252, 252, 252, 252, 252, 252, 252, 252, 1, 1, 1, 1, 1, 1, 1, 1, 253, 253, 253, 253, 253, 253, 253, 253, 254, 254, 254, 254, 254, 254, 254, 255,
        256, 256, 256, 256, 256, 256, 256, 257, 258, 258, 258, 258, 258, 258, 258, 258, 259, 259, 259, 259, 259, 259, 259, 259, 260, 261, 262, 263, 264, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        265, 265, 265, 265, 265, 265, 265, 265, 1, 1, 1, 1, 1, 1, 1, 1, 266, 266, 266, 266, 266, 266, 266, 266, 267, 267, 267, 267, 267, 267, 267, 268,
        269, 269, 269, 269, 269, 269, 269, 270, 271, 271, 271, 271, 271, 271, 271, 271, 272, 272, 272, 272, 272, 272, 272, 272, 273, 274, 275, 276, 277, 1, 1, 1,
        278, 278, 278, 278, 278, 278, 278, 278, 1, 1, 1, 1, 1, 1, 1, 1, 279, 279, 279, 279, 279, 279, 279, 279, 280, 280, 280, 280, 280, 280, 280, 281,
        282, 282, 282, 282, 282, 282, 282, 283, 284, 284, 284, 284, 284, 284, 284, 284, 285, 285, 285, 285, 285, 285, 285, 285, 286, 287, 288, 289, 290, 1, 1, 1,
        291, 291, 291, 291, 291, 291, 291, 291, 1, 1, 1, 1, 1, 1, 1, 1, 292, 292, 292, 292, 292, 292, 292, 292, 293, 293, 293, 293, 293, 293, 293, 294,
        295, 295, 295, 295, 295, 295, 295, 296, 297, 297, 297, 297, 297, 297, 297, 297, 298, 298, 298, 298, 298, 298, 298, 298, 299, 300, 301, 302, 303, 1, 1, 1,
        304, 304, 304, 304, 304, 304, 304, 304, 1, 1, 1, 1, 1, 1, 1, 1, 305, 305, 305, 305, 305, 305, 305, 305, 306, 306, 306, 306, 306, 306, 306, 307,
        308, 308, 308, 308, 308, 308, 308, 309, 310, 310, 310, 310, 310, 310, 310, 310, 311, 311, 311, 311, 311, 311, 311, 311, 312, 313, 314, 315, 316, 1, 1, 1,
        317, 317, 317, 317, 317, 317, 317, 317, 1, 1, 1, 1, 1, 1, 1, 1, 318, 318, 318, 318, 318, 318, 318, 318, 319, 319, 319, 319, 319, 319, 319, 320,
        321, 321, 321, 321, 321, 321, 321, 322, 323, 323, 323, 323, 323, 323, 323, 323, 324, 324, 324, 324, 324, 324, 324, 324, 325, 326, 327, 328, 329, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        252, 252, 252, 252, 252, 252, 252, 252, 1, 1, 1, 1, 1, 1, 1, 1, 253, 253, 253, 253, 253, 253, 253, 253, 254, 254, 254, 254, 254, 254, 254, 255,
        256, 256, 256, 256, 256, 256, 256, 257, 258, 258, 258, 258, 258, 258, 258, 258, 259, 259, 259, 259, 259, 259, 259, 259, 260, 261, 262, 263, 264, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        265, 265, 265, 265, 265, 265, 265, 265, 1, 1, 1, 1, 1, 1, 1, 1, 266, 266, 266, 266, 266, 266, 266, 266, 267, 267, 267, 267, 267, 267, 267, 268,
        269, 269, 269, 269, 269, 269, 269, 270, 271, 271, 271, 271, 271, 271, 271, 271, 272, 272, 272, 272, 272, 272, 272, 272, 273, 274, 275, 276, 277, 1, 1, 1,
        278, 278, 278, 278, 278, 278, 278, 278, 1, 1, 1, 1, 1, 1, 1, 1, 279, 279, 279, 279, 279, 279, 279, 279, 280, 280, 280, 280, 280, 280, 280, 281,
        282, 282, 282, 282, 282, 282, 282, 283, 284, 284, 284, 284, 284, 284, 284, 284, 285, 285, 285, 285, 285, 285, 285, 285, 286, 287, 288, 289, 290, 1, 1, 1,
        291, 291, 291, 291, 291, 291, 291, 291, 1, 1, 1, 1, 1, 1, 1, 1, 292, 292, 292, 292, 292, 292, 292, 292, 293, 293, 293, 293, 293, 293, 293, 294,
        295, 295, 295, 295, 295, 295, 295, 296, 297, 297, 297, 297, 297, 297, 297, 297, 298, 298, 298, 298, 298, 298, 298, 298, 299, 300, 301, 302, 303, 1, 1, 1,
        304, 304, 304, 304, 304, 304, 304, 304, 1, 1, 1, 1, 1, 1, 1, 1, 305, 305, 305, 305, 305, 305, 305, 305, 306, 306, 306, 306, 306, 306, 306, 307,
        308, 308, 308, 308, 308, 308, 308, 309, 310, 310, 310, 310, 310, 310, 310, 310, 311, 311, 311, 311, 311, 311, 311, 311, 312, 313, 314, 315, 316, 1, 1, 1,
        317, 317, 317, 317, 317, 317, 317, 317, 1, 1, 1, 1, 1, 1, 1, 1, 318, 318, 318, 318, 318, 318, 318, 318, 319, 319, 319, 319, 319, 319, 319, 320,
        321, 321, 321, 321, 321, 321, 321, 322, 323, 323, 323, 323, 323, 323, 323, 323, 324, 324, 324, 324, 324, 324, 324, 324, 325, 326, 327, 328, 329, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        252, 252, 252, 252, 252, 252, 252, 252, 1, 1, 1, 1, 1, 1, 1, 1, 253, 253, 253, 253, 253, 253, 253, 253, 254, 254, 254, 254, 254, 254, 254, 255,
        256, 256, 256, 256, 256, 256, 256, 257, 258, 258, 258, 258, 258, 258, 258, 258, 259, 259, 259, 259, 259, 259, 259, 259, 260, 261, 262, 263, 264, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        265, 265, 265, 265, 265, 265, 265, 265, 1, 1, 1, 1, 1, 1, 1, 1, 266, 266, 266, 266, 266, 266, 266, 266, 267, 267, 267, 267, 267, 267, 267, 268,
        269, 269, 269, 269, 269, 269, 269, 270, 271, 271, 271, 271, 271, 271, 271, 271, 272, 272, 272, 272, 272, 272, 272, 272, 273, 274, 275, 276, 277, 1, 1, 1,
        278, 278, 278, 278, 278, 278, 278, 278, 1, 1, 1, 1, 1, 1, 1, 1, 279, 279, 279, 279, 279, 279, 279, 279, 280, 280, 280, 280, 280, 280, 280, 281,
        282, 282, 282, 282, 282, 282, 282, 283, 284, 284, 284, 284, 284, 284, 284, 284, 285, 285, 285, 285, 285, 285, 285, 285, 286, 287, 288, 289, 290, 1, 1, 1,
        291, 291, 291, 291, 291, 291, 291, 291, 1, 1, 1, 1, 1, 1, 1, 1, 292, 292, 292, 292, 292, 292, 292, 292, 293, 293, 293, 293, 293, 293, 293, 294,
        295, 295, 295, 295, 295, 295, 295, 296, 297, 297, 297, 297, 297, 297, 297, 297, 298, 298, 298, 298, 298, 298, 298, 298, 299, 300, 301, 302, 303, 1, 1, 1,
        304, 304, 304, 304, 304, 304, 304, 304, 1, 1, 1, 1, 1, 1, 1, 1, 305, 305, 305, 305, 305, 305, 305, 305, 306, 306, 306, 306, 306, 306, 306, 307,
        308, 308, 308, 308, 308, 308, 308, 309, 310, 310, 310, 310, 310, 310, 310, 310, 311, 311, 311, 311, 311, 311, 311, 311, 312, 313, 314, 315, 316, 1, 1, 1,
        317, 317, 317, 317, 317, 317, 317, 317, 1, 1, 1, 1, 1, 1, 1, 1, 318, 318, 318, 318, 318, 318, 318, 318, 319, 319, 319, 319, 319, 319, 319, 320,
        321, 321, 321, 321, 321, 321, 321, 322, 323, 323, 323, 323, 323, 323, 323, 323, 324, 324, 324, 324, 324, 324, 324, 324, 325, 326, 327, 328, 329, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        252, 252, 252, 252, 252, 252, 252, 252, 1, 1, 1, 1, 1, 1, 1, 1, 253, 253, 253, 253, 253, 253, 253, 253, 254, 254, 254, 254, 254, 254, 254, 255,
        256, 256, 256, 256, 256, 256, 256, 257, 258, 258, 258, 258, 258, 258, 258, 258, 259, 259, 259, 259, 259, 259, 259, 259, 260, 261, 262, 263, 264, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        265, 265, 265, 265, 265, 265, 265, 265, 1, 1, 1, 1, 1, 1, 1, 1, 266, 266, 266, 266, 266, 266, 266, 266, 267, 267, 267, 267, 267, 267, 267, 268,
        269, 269, 269, 269, 269, 269, 269, 270, 271, 271, 271, 271, 271, 271, 271, 271, 272, 272, 272, 272, 272, 272, 272, 272, 273, 274, 275, 276, 277, 1, 1, 1,
        356, 356, 356, 356, 356, 356, 356, 356, 1, 1, 1, 1, 1, 1, 1, 1, 357, 357, 357, 357, 357, 357, 357, 357, 358, 358, 358, 358, 358, 358, 358, 359,
        360, 360, 360, 360, 360, 360, 360, 361, 362, 362, 362, 362, 362, 362, 362, 362, 363, 363, 363, 363, 363, 363, 363, 363, 364, 365, 366, 367, 368, 1, 1, 1,
        369, 369, 369, 369, 369, 369, 369, 369, 1, 1, 1, 1, 1, 1, 1, 1, 370, 370, 370, 370, 370, 370, 370, 370, 371, 371, 371, 371, 371, 371, 371, 372,
        373, 373, 373, 373, 373, 373, 373, 374, 375, 375, 375, 375, 375, 375, 375, 375, 376, 376, 376, 376, 376, 376, 376, 376, 377, 378, 379, 380, 381, 1, 1, 1,
        304, 304, 304, 304, 304, 304, 304, 304, 1, 1, 1, 1, 1, 1, 1, 1, 305, 305, 305, 305, 305, 305, 305, 305, 306, 306, 306, 306, 306, 306, 306, 307,
        308, 308, 308, 308, 308, 308, 308, 309, 310, 310, 310, 310, 310, 310, 310, 310, 311, 311, 311, 311, 311, 311, 311, 311, 312, 313, 314, 315, 316, 1, 1, 1,
        317, 317, 317, 317, 317, 317, 317, 317, 1, 1, 1, 1, 1, 1, 1, 1, 318, 318, 318, 318, 318, 318, 318, 318, 319, 319, 319, 319, 319, 319, 319, 320,
        321, 321, 321, 321, 321, 321, 321, 322, 323, 323, 323, 323, 323, 323, 323, 323, 324, 324, 324, 324, 324, 324, 324, 324, 325, 326, 327, 328, 329, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        382, 382, 382, 382, 382, 382, 382, 382, 383, 383, 383, 383, 383, 383, 383, 383, 384, 384, 384, 384, 384, 384, 384, 384, 385, 385, 385, 385, 385, 385, 385, 385,
        386, 386, 386, 386, 386, 386, 386, 386, 387, 387, 387, 387, 387, 387, 387, 387, 388, 388, 388, 388, 388, 388, 388, 388, 389, 390, 391, 392, 393, 1, 1, 1,
        394, 394, 394, 394, 394, 394, 394, 394, 395, 395, 395, 395, 395, 395, 395, 395, 396, 396, 396, 396, 396, 396, 396, 396, 397, 397, 397, 397, 397, 397, 397, 397,
        398, 398, 398, 398, 398, 398, 398, 398, 399, 399, 399, 399, 399, 399, 399, 399, 400, 400, 400, 400, 400, 400, 400, 400, 401, 402, 403, 404, 405, 1, 1, 1,
        406, 406, 406, 406, 406, 406, 406, 406, 407, 407, 407, 407, 407, 407, 407, 407, 408, 408, 408, 408, 408, 408, 408, 408, 409, 409, 409, 409, 409, 409, 409, 409,
        410, 410, 410, 410, 410, 410, 410, 410, 411, 411, 411, 411, 411, 411, 411, 411, 412, 412, 412, 412, 412, 412, 412, 412, 413, 414, 415, 416, 417, 1, 1, 1,
        418, 418, 418, 418, 418, 418, 418, 418, 419, 419, 419, 419, 419, 419, 419, 419, 420, 420, 420, 420, 420, 420, 420, 420, 421, 421, 421, 421, 421, 421, 421, 421,
        422, 422, 422, 422, 422, 422, 422, 422, 423, 423, 423, 423, 423, 423, 423, 423, 424, 424, 424, 424, 424, 424, 424, 424, 425, 426, 427, 428, 429, 1, 1, 1,
        430, 430, 430, 430, 430, 430, 430, 430, 431, 431, 431, 431, 431, 431, 431, 431, 432, 432, 432, 432, 432, 432, 432, 432, 433, 433, 433, 433, 433, 433, 433, 433,
        434, 434, 434, 434, 434, 434, 434, 434, 435, 435, 435, 435, 435, 435, 435, 435, 436, 436, 436, 436, 436, 436, 436, 436, 437, 438, 439, 440, 441, 1, 1, 1,
        442, 442, 442, 442, 442, 442, 442, 442, 443, 443, 443, 443, 443, 443, 443, 443, 444, 444, 444, 444, 444, 444, 444, 444, 445, 445, 445, 445, 445, 445, 445, 445,
        446, 446, 446, 446, 446, 446, 446, 446, 447, 447, 447, 447, 447, 447, 447, 447, 448, 448, 448, 448, 448, 448, 448, 448, 449, 450, 451, 452, 453, 1, 1, 1,
        454, 454, 454, 454, 454, 454, 454, 454, 455, 455, 455, 455, 455, 455, 455, 455, 456, 456, 456, 456, 456, 456, 456, 456, 457, 457, 457, 457, 457, 457, 457, 457,
        458, 458, 458, 458, 458, 458, 458, 458, 459, 459, 459, 459, 459, 459, 459, 459, 460, 460, 460, 460, 460, 460, 460, 460, 461, 462, 463, 464, 465, 1, 1, 1,
        466, 466, 466, 466, 466, 466, 466, 466, 467, 467, 467, 467, 467, 467, 467, 467, 468, 468, 468, 468, 468, 468, 468, 468, 469, 469, 469, 469, 469, 469, 469, 469,
        470, 470, 470, 470, 470, 470, 470, 470, 471, 471, 471, 471, 471, 471, 471, 471, 472, 472, 472, 472, 472, 472, 472, 472, 473, 474, 475, 476, 477, 1, 1, 1,
        382, 382, 382, 382, 382, 382, 382, 382, 383, 383, 383, 383, 383, 383, 383, 383, 384, 384, 384, 384, 384, 384, 384, 384, 385, 385, 385, 385, 385, 385, 385, 385,
        386, 386, 386, 386, 386, 386, 386, 386, 387, 387, 387, 387, 387, 387, 387, 387, 388, 388, 388, 388, 388, 388, 388, 388, 389, 390, 391, 392, 393, 1, 1, 1,
        394, 394, 394, 394, 394, 394, 394, 394, 395, 395, 395, 395, 395, 395, 395, 395, 396, 396, 396, 396, 396, 396, 396, 396, 397, 397, 397, 397, 397, 397, 397, 397,
        398, 398, 398, 398, 398, 398, 398, 398, 399, 399, 399, 399, 399, 399, 399, 399, 400, 400, 400, 400, 400, 400, 400, 400, 401, 402, 403, 404, 405, 1, 1, 1,
        406, 406, 406, 406, 406, 406, 406, 406, 407, 407, 407, 407, 407, 407, 407, 407, 408, 408, 408, 408, 408, 408, 408, 408, 409, 409, 409, 409, 409, 409, 409, 409,
        410, 410, 410, 410, 410, 410, 410, 410, 411, 411, 411, 411, 411, 411, 411, 411, 412, 412, 412, 412, 412, 412, 412, 412, 413, 414, 415, 416, 417, 1, 1, 1,
        418, 418, 418, 418, 418, 418, 418, 418, 419, 419, 419, 419, 419, 419, 419, 419, 420, 420, 420, 420, 420, 420, 420, 420, 421, 421, 421, 421, 421, 421, 421, 421,
        422, 422, 422, 422, 422, 422, 422, 422, 423, 423, 423, 423, 423, 423, 423, 423, 424, 424, 424, 424, 424, 424, 424, 424, 425, 426, 427, 428, 429, 1, 1, 1,
        430, 430, 430, 430, 430, 430, 430, 430, 431, 431, 431, 431, 431, 431, 431, 431, 432, 432, 432, 432, 432, 432, 432, 432, 433, 433, 433, 433, 433, 433, 433, 433,
        434, 434, 434, 434, 434, 434, 434, 434, 435, 435, 435, 435, 435, 435, 435, 435, 436, 436, 436, 436, 436, 436, 436, 436, 437, 438, 439, 440, 441, 1, 1, 1,
        442, 442, 442, 442, 442, 442, 442, 442, 443, 443, 443, 443, 443, 443, 443, 443, 444, 444, 444, 444, 444, 444, 444, 444, 445, 445, 445, 445, 445, 445, 445, 445,
        446, 446, 446, 446, 446, 446, 446, 446, 447, 447, 447, 447, 447, 447, 447, 447, 448, 448, 448, 448, 448, 448, 448, 448, 449, 450, 451, 452, 453, 1, 1, 1,
        454, 454, 454, 454, 454, 454, 454, 454, 455, 455, 455, 455, 455, 455, 455, 455, 456, 456, 456, 456, 456, 456, 456, 456, 457, 457, 457, 457, 457, 457, 457, 457,
        458, 458, 458, 458, 458, 458, 458, 458, 459, 459, 459, 459, 459, 459, 459, 459, 460, 460, 460, 460, 460, 460, 460, 460, 461, 462, 463, 464, 465, 1, 1, 1,
        478, 478, 478, 478, 478, 478, 478, 478, 479, 479, 479, 479, 479, 479, 479, 479, 480, 480, 480, 480, 480, 480, 480, 480, 481, 481, 481, 481, 481, 481, 481, 481,
        482, 482, 482, 482, 482, 482, 482, 482, 483, 483, 483, 483, 483, 483, 483, 483, 484, 484, 484, 484, 484, 484, 484, 484, 485, 486, 487, 488, 489, 1, 1, 1,
        382, 382, 382, 382, 382, 382, 382, 382, 383, 383, 383, 383, 383, 383, 383, 383, 384, 384, 384, 384, 384, 384, 384, 384, 385, 385, 385, 385, 385, 385, 385, 385,
        386, 386, 386, 386, 386, 386, 386, 386, 387, 387, 387, 387, 387, 387, 387, 387, 388, 388, 388, 388, 388, 388, 388, 388, 389, 390, 391, 392, 393, 1, 1, 1,
        394, 394, 394, 394, 394, 394, 394, 394, 395, 395, 395, 395, 395, 395, 395, 395, 396, 396, 396, 396, 396, 396, 396, 396, 397, 397, 397, 397, 397, 397, 397, 397,
        398, 398, 398, 398, 398, 398, 398, 398, 399, 399, 399, 399, 399, 399, 399, 399, 400, 400, 400, 400, 400, 400, 400, 400, 401, 402, 403, 404, 405, 1, 1, 1,
        406, 406, 406, 406, 406, 406, 406, 406, 407, 407, 407, 407, 407, 407, 407, 407, 408, 408, 408, 408, 408, 408, 408, 408, 409, 409, 409, 409, 409, 409, 409, 409,
        410, 410, 410, 410, 410, 410, 410, 410, 411, 411, 411, 411, 411, 411, 411, 411, 412, 412, 412, 412, 412, 412, 412, 412, 413, 414, 415, 416, 417, 1, 1, 1,
        418, 418, 418, 418, 418, 418, 418, 418, 419, 419, 419, 419, 419, 419, 419, 419, 420, 420, 420, 420, 420, 420, 420, 420, 421, 421, 421, 421, 421, 421, 421, 421,
        422, 422, 422, 422, 422, 422, 422, 422, 423, 423, 423, 423, 423, 423, 423, 423, 424, 424, 424, 424, 424, 424, 424, 424, 425, 426, 427, 428, 429, 1, 1, 1,
        430, 430, 430, 430, 430, 430, 430, 430, 431, 431, 431, 431, 431, 431, 431, 431, 432, 432, 432, 432, 432, 432, 432, 432, 433, 433, 433, 433, 433, 433, 433, 433,
        434, 434, 434, 434, 434, 434, 434, 434, 435, 435, 435, 435, 435, 435, 435, 435, 436, 436, 436, 436, 436, 436, 436, 436, 437, 438, 439, 440, 441, 1, 1, 1,
        442, 442, 442, 442, 442, 442, 442, 442, 443, 443, 443, 443, 443, 443, 443, 443, 444, 444, 444, 444, 444, 444, 444, 444, 445, 445, 445, 445, 445, 445, 445, 445,
        446, 446, 446, 446, 446, 446, 446, 446, 447, 447, 447, 447, 447, 447, 447, 447, 448, 448, 448, 448, 448, 448, 448, 448, 449, 450, 451, 452, 453, 1, 1, 1,
        454, 454, 454, 454, 454, 454, 454, 454, 455, 455, 455, 455, 455, 455, 455, 455, 456, 456, 456, 456, 456, 456, 456, 456, 457, 457, 457, 457, 457, 457, 457, 457,
        458, 458, 458, 458, 458, 458, 458, 458, 459, 459, 459, 459, 459, 459, 459, 459, 460, 460, 460, 460, 460, 460, 460, 460, 461, 462, 463, 464, 465, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        382, 382, 382, 382, 382, 382, 382, 382, 383, 383, 383, 383, 383, 383, 383, 383, 384, 384, 384, 384, 384, 384, 384, 384, 385, 385, 385, 385, 385, 385, 385, 385,
        386, 386, 386, 386, 386, 386, 386, 386, 387, 387, 387, 387, 387, 387, 387, 387, 388, 388, 388, 388, 388, 388, 388, 388, 389, 390, 391, 392, 393, 1, 1, 1,
        394, 394, 394, 394, 394, 394, 394, 394, 395, 395, 395, 395, 395, 395, 395, 395, 396, 396, 396, 396, 396, 396, 396, 396, 397, 397, 397, 397, 397, 397, 397, 397,
        398, 398, 398, 398, 398, 398, 398, 398, 399, 399, 399, 399, 399, 399, 399, 399, 400, 400, 400, 400, 400, 400, 400, 400, 401, 402, 403, 404, 405, 1, 1, 1,
        406, 406, 406, 406, 406, 406, 406, 406, 407, 407, 407, 407, 407, 407, 407, 407, 408, 408, 408, 408, 408, 408, 408, 408, 409, 409, 409, 409, 409, 409, 409, 409,
        410, 410, 410, 410, 410, 410, 410, 410, 411, 411, 411, 411, 411, 411, 411, 411, 412, 412, 412, 412, 412, 412, 412, 412, 413, 414, 415, 416, 417, 1, 1, 1,
        418, 418, 418, 418, 418, 418, 418, 418, 419, 419, 419, 419, 419, 419, 419, 419, 420, 420, 420, 420, 420, 420, 420, 420, 421, 421, 421, 421, 421, 421, 421, 421,
        422, 422, 422, 422, 422, 422, 422, 422, 423, 423, 423, 423, 423, 423, 423, 423, 424, 424, 424, 424, 424, 424, 424, 424, 425, 426, 427, 428, 429, 1, 1, 1,
        430, 430, 430, 430, 430, 430, 430, 430, 431, 431, 431, 431, 431, 431, 431, 431, 432, 432, 432, 432, 432, 432, 432, 432, 433, 433, 433, 433, 433, 433, 433, 433,
        434, 434, 434, 434, 434, 434, 434, 434, 435, 435, 435, 435, 435, 435, 435, 435, 436, 436, 436, 436, 436, 436, 436, 436, 437, 438, 439, 440, 441, 1, 1, 1,
        442, 442, 442, 442, 442, 442, 442, 442, 443, 443, 443, 443, 443, 443, 443, 443, 444, 444, 444, 444, 444, 444, 444, 444, 445, 445, 445, 445, 445, 445, 445, 445,
        446, 446, 446, 446, 446, 446, 446, 446, 447, 447, 447, 447, 447, 447, 447, 447, 448, 448, 448, 448, 448, 448, 448, 448, 449, 450, 451, 452, 453, 1, 1, 1,
        454, 454, 454, 454, 454, 454, 454, 454, 455, 455, 455, 455, 455, 455, 455, 455, 456, 456, 456, 456, 456, 456, 456, 456, 457, 457, 457, 457, 457, 457, 457, 457,
        458, 458, 458, 458, 458, 458, 458, 458, 459, 459, 459, 459, 459, 459, 459, 459, 460, 460, 460, 460, 460, 460, 460, 460, 461, 462, 463, 464, 465, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        382, 382, 382, 382, 382, 382, 382, 382, 383, 383, 383, 383, 383, 383, 383, 383, 384, 384, 384, 384, 384, 384, 384, 384, 385, 385, 385, 385, 385, 385, 385, 385,
        386, 386, 386, 386, 386, 386, 386, 386, 387, 387, 387, 387, 387, 387, 387, 387, 388, 388, 388, 388, 388, 388, 388, 388, 389, 390, 391, 392, 393, 1, 1, 1,
        394, 394, 394, 394, 394, 394, 394, 394, 395, 395, 395, 395, 395, 395, 395, 395, 396, 396, 396, 396, 396, 396, 396, 396, 397, 397, 397, 397, 397, 397, 397, 397,
        398, 398, 398, 398, 398, 398, 398, 398, 399, 399, 399, 399, 399, 399, 399, 399, 400, 400, 400, 400, 400, 400, 400, 400, 401, 402, 403, 404, 405, 1, 1, 1,
        406, 406, 406, 406, 406, 406, 406, 406, 407, 407, 407, 407, 407, 407, 407, 407, 408, 408, 408, 408, 408, 408, 408, 408, 409, 409, 409, 409, 409, 409, 409, 409,
        410, 410, 410, 410, 410, 410, 410, 410, 411, 411, 411, 411, 411, 411, 411, 411, 412, 412, 412, 412, 412, 412, 412, 412, 413, 414, 415, 416, 417, 1, 1, 1,
        418, 418, 418, 418, 418, 418, 418, 418, 419, 419, 419, 419, 419, 419, 419, 419, 420, 420, 420, 420, 420, 420, 420, 420, 421, 421, 421, 421, 421, 421, 421, 421,
        422, 422, 422, 422, 422, 422, 422, 422, 423, 423, 423, 423, 423, 423, 423, 423, 424, 424, 424, 424, 424, 424, 424, 424, 425, 426, 427, 428, 429, 1, 1, 1,
        430, 430, 430, 430, 430, 430, 430, 430, 431, 431, 431, 431, 431, 431, 431, 431, 432, 432, 432, 432, 432, 432, 432, 432, 433, 433, 433, 433, 433, 433, 433, 433,
        434, 434, 434, 434, 434, 434, 434, 434, 435, 435, 435, 435, 435, 435, 435, 435, 436, 436, 436, 436, 436, 436, 436, 436, 437, 438, 439, 440, 441, 1, 1, 1,
        442, 442, 442, 442, 442, 442, 442, 442, 443, 443, 443, 443, 443, 443, 443, 443, 444, 444, 444, 444, 444, 444, 444, 444, 445, 445, 445, 445, 445, 445, 445, 445,
        446, 446, 446, 446, 446, 446, 446, 446, 447, 447, 447, 447, 447, 447, 447, 447, 448, 448, 448, 448, 448, 448, 448, 448, 449, 450, 451, 452, 453, 1, 1, 1,
        454, 454, 454, 454, 454, 454, 454, 454, 455, 455, 455, 455, 455, 455, 455, 455, 456, 456, 456, 456, 456, 456, 456, 456, 457, 457, 457, 457, 457, 457, 457, 457,
        458, 458, 458, 458, 458, 458, 458, 458, 459, 459, 459, 459, 459, 459, 459, 459, 460, 460, 460, 460, 460, 460, 460, 460, 461, 462, 463, 464, 465, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        382, 382, 382, 382, 382, 382, 382, 382, 383, 383, 383, 383, 383, 383, 383, 383, 384, 384, 384, 384, 384, 384, 384, 384, 385, 385, 385, 385, 385, 385, 385, 385,
        386, 386, 386, 386, 386, 386, 386, 386, 387, 387, 387, 387, 387, 387, 387, 387, 388, 388, 388, 388, 388, 388, 388, 388, 389, 390, 391, 392, 393, 1, 1, 1,
        394, 394, 394, 394, 394, 394, 394, 394, 395, 395, 395, 395, 395, 395, 395, 395, 396, 396, 396, 396, 396, 396, 396, 396, 397, 397, 397, 397, 397, 397, 397, 397,
        398, 398, 398, 398, 398, 398, 398, 398, 399, 399, 399, 399, 399, 399, 399, 399, 400, 400, 400, 400, 400, 400, 400, 400, 401, 402, 403, 404, 405, 1, 1, 1,
        406, 406, 406, 406, 406, 406, 406, 406, 407, 407, 407, 407, 407, 407, 407, 407, 408, 408, 408, 408, 408, 408, 408, 408, 409, 409, 409, 409, 409, 409, 409, 409,
        410, 410, 410, 410, 410, 410, 410, 410, 411, 411, 411, 411, 411, 411, 411, 411, 412, 412, 412, 412, 412, 412, 412, 412, 413, 414, 415, 416, 417, 1, 1, 1,
        418, 418, 418, 418, 418, 418, 418, 418, 419, 419, 419, 419, 419, 419, 419, 419, 420, 420, 420, 420, 420, 420, 420, 420, 421, 421, 421, 421, 421, 421, 421, 421,
        422, 422, 422, 422, 422, 422, 422, 422, 423, 423, 423, 423, 423, 423, 423, 423, 424, 424, 424, 424, 424, 424, 424, 424, 425, 426, 427, 428, 429, 1, 1, 1,
        430, 430, 430, 430, 430, 430, 430, 430, 431, 431, 431, 431, 431, 431, 431, 431, 432, 432, 432, 432, 432, 432, 432, 432, 433, 433, 433, 433, 433, 433, 433, 433,
        434, 434, 434, 434, 434, 434, 434, 434, 435, 435, 435, 435, 435, 435, 435, 435, 436, 436, 436, 436, 436, 436, 436, 436, 437, 438, 439, 440, 441, 1, 1, 1,
        442, 442, 442, 442, 442, 442, 442, 442, 443, 443, 443, 443, 443, 443, 443, 443, 444, 444, 444, 444, 444, 444, 444, 444, 445, 445, 445, 445, 445, 445, 445, 445,
        446, 446, 446, 446, 446, 446, 446, 446, 447, 447, 447, 447, 447, 447, 447, 447, 448, 448, 448, 448, 448, 448, 448, 448, 449, 450, 451, 452, 453, 1, 1, 1,
        454, 454, 454, 454, 454, 454, 454, 454, 455, 455, 455, 455, 455, 455, 455, 455, 456, 456, 456, 456, 456, 456, 456, 456, 457, 457, 457, 457, 457, 457, 457, 457,
        458, 458, 458, 458, 458, 458, 458, 458, 459, 459, 459, 459, 459, 459, 459, 459, 460, 460, 460, 460, 460, 460, 460, 460, 461, 462, 463, 464, 465, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        382, 382, 382, 382, 382, 382, 382, 382, 383, 383, 383, 383, 383, 383, 383, 383, 384, 384, 384, 384, 384, 384, 384, 384, 385, 385, 385, 385, 385, 385, 385, 385,
        386, 386, 386, 386, 386, 386, 386, 386, 387, 387, 387, 387, 387, 387, 387, 387, 388, 388, 388, 388, 388, 388, 388, 388, 389, 390, 391, 392, 393, 1, 1, 1,
        394, 394, 394, 394, 394, 394, 394, 394, 395, 395, 395, 395, 395, 395, 395, 395, 396, 396, 396, 396, 396, 396, 396, 396, 397, 397, 397, 397, 397, 397, 397, 397,
        398, 398, 398, 398, 398, 398, 398, 398, 399, 399, 399, 399, 399, 399, 399, 399, 400, 400, 400, 400, 400, 400, 400, 400, 401, 402, 403, 404, 405, 1, 1, 1,
        406, 406, 406, 406, 406, 406, 406, 406, 407, 407, 407, 407, 407, 407, 407, 407, 408, 408, 408, 408, 408, 408, 408, 408, 409, 409, 409, 409, 409, 409, 409, 409,
        410, 410, 410, 410, 410, 410, 410, 410, 411, 411, 411, 411, 411, 411, 411, 411, 412, 412, 412, 412, 412, 412, 412, 412, 413, 414, 415, 416, 417, 1, 1, 1,
        418, 418, 418, 418, 418, 418, 418, 418, 419, 419, 419, 419, 419, 419, 419, 419, 420, 420, 420, 420, 420, 420, 420, 420, 421, 421, 421, 421, 421, 421, 421, 421,
        422, 422, 422, 422, 422, 422, 422, 422, 423, 423, 423, 423, 423, 423, 423, 423, 424, 424, 424, 424, 424, 424, 424, 424, 425, 426, 427, 428, 429, 1, 1, 1,
        430, 430, 430, 430, 430, 430, 430, 430, 431, 431, 431, 431, 431, 431, 431, 431, 432, 432, 432, 432, 432, 432, 432, 432, 433, 433, 433, 433, 433, 433, 433, 433,
        434, 434, 434, 434, 434, 434, 434, 434, 435, 435, 435, 435, 435, 435, 435, 435, 436, 436, 436, 436, 436, 436, 436, 436, 437, 438, 439, 440, 441, 1, 1, 1,
        442, 442, 442, 442, 442, 442, 442, 442, 443, 443, 443, 443, 443, 443, 443, 443, 444, 444, 444, 444, 444, 444, 444, 444, 445, 445, 445, 445, 445, 445, 445, 445,
        446, 446, 446, 446, 446, 446, 446, 446, 447, 447, 447, 447, 447, 447, 447, 447, 448, 448, 448, 448, 448, 448, 448, 448, 449, 450, 451, 452, 453, 1, 1, 1,
        454, 454, 454, 454, 454, 454, 454, 454, 455, 455, 455, 455, 455, 455, 455, 455, 456, 456, 456, 456, 456, 456, 456, 456, 457, 457, 457, 457, 457, 457, 457, 457,
        458, 458, 458, 458, 458, 458, 458, 458, 459, 459, 459, 459, 459, 459, 459, 459, 460, 460, 460, 460, 460, 460, 460, 460, 461, 462, 463, 464, 465, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        382, 382, 382, 382, 382, 382, 382, 382, 383, 383, 383, 383, 383, 383, 383, 383, 384, 384, 384, 384, 384, 384, 384, 384, 385, 385, 385, 385, 385, 385, 385, 385,
        386, 386, 386, 386, 386, 386, 386, 386, 387, 387, 387, 387, 387, 387, 387, 387, 388, 388, 388, 388, 388, 388, 388, 388, 389, 390, 391, 392, 393, 1, 1, 1,
        394, 394, 394, 394, 394, 394, 394, 394, 395, 395, 395, 395, 395, 395, 395, 395, 396, 396, 396, 396, 396, 396, 396, 396, 397, 397, 397, 397, 397, 397, 397, 397,
        398, 398, 398, 398, 398, 398, 398, 398, 399, 399, 399, 399, 399, 399, 399, 399, 400, 400, 400, 400, 400, 400, 400, 400, 401, 402, 403, 404, 405, 1, 1, 1,
        406, 406, 406, 406, 406, 406, 406, 406, 407, 407, 407, 407, 407, 407, 407, 407, 408, 408, 408, 408, 408, 408, 408, 408, 409, 409, 409, 409, 409, 409, 409, 409,
        410, 410, 410, 410, 410, 410, 410, 410, 411, 411, 411, 411, 411, 411, 411, 411, 412, 412, 412, 412, 412, 412, 412, 412, 413, 414, 415, 416, 417, 1, 1, 1,
        418, 418, 418, 418, 418, 418, 418, 418, 419, 419, 419, 419, 419, 419, 419, 419, 420, 420, 420, 420, 420, 420, 420, 420, 421, 421, 421, 421, 421, 421, 421, 421,
        422, 422, 422, 422, 422, 422, 422, 422, 423, 423, 423, 423, 423, 423, 423, 423, 424, 424, 424, 424, 424, 424, 424, 424, 425, 426, 427, 428, 429, 1, 1, 1,
        430, 430, 430, 430, 430, 430, 430, 430, 431, 431, 431, 431, 431, 431, 431, 431, 432, 432, 432, 432, 432, 432, 432, 432, 433, 433, 433, 433, 433, 433, 433, 433,
        434, 434, 434, 434, 434, 434, 434, 434, 435, 435, 435, 435, 435, 435, 435, 435, 436, 436, 436, 436, 436, 436, 436, 436, 437, 438, 439, 440, 441, 1, 1, 1,
        442, 442, 442, 442, 442, 442, 442, 442, 443, 443, 443, 443, 443, 443, 443, 443, 444, 444, 444, 444, 444, 444, 444, 444, 445, 445, 445, 445, 445, 445, 445, 445,
        446, 446, 446, 446, 446, 446, 446, 446, 447, 447, 447, 447, 447, 447, 447, 447, 448, 448, 448, 448, 448, 448, 448, 448, 449, 450, 451, 452, 453, 1, 1, 1,
        454, 454, 454, 454, 454, 454, 454, 454, 455, 455, 455, 455, 455, 455, 455, 455, 456, 456, 456, 456, 456, 456, 456, 456, 457, 457, 457, 457, 457, 457, 457, 457,
        458, 458, 458, 458, 458, 458, 458, 458, 459, 459, 459, 459, 459, 459, 459, 459, 460, 460, 460, 460, 460, 460, 460, 460, 461, 462, 463, 464, 465, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        490, 490, 490, 490, 490, 490, 490, 490, 491, 491, 491, 491, 491, 491, 491, 491, 492, 492, 492, 492, 492, 492, 492, 492, 493, 493, 493, 493, 493, 493, 493, 493,
        494, 494, 494, 494, 494, 494, 494, 494, 495, 495, 495, 495, 495, 495, 495, 495, 496, 496, 496, 496, 496, 496, 496, 496, 497, 498, 499, 500, 501, 1, 1, 1,
        502, 502, 502, 502, 502, 502, 502, 502, 503, 503, 503, 503, 503, 503, 503, 503, 504, 504, 504, 504, 504, 504, 504, 504, 505, 505, 505, 505, 505, 505, 505, 505,
        506, 506, 506, 506, 506, 506, 506, 506, 507, 507, 507, 507, 507, 507, 507, 507, 508, 508, 508, 508, 508, 508, 508, 508, 509, 510, 511, 512, 513, 1, 1, 1,
        514, 514, 514, 514, 514, 514, 514, 514, 515, 515, 515, 515, 515, 515, 515, 515, 516, 516, 516, 516, 516, 516, 516, 516, 517, 517, 517, 517, 517, 517, 517, 517,
        518, 518, 518, 518, 518, 518, 518, 518, 519, 519, 519, 519, 519, 519, 519, 519, 520, 520, 520, 520, 520, 520, 520, 520, 521, 522, 523, 524, 525, 1, 1, 1,
        526, 526, 526, 526, 526, 526, 526, 526, 527, 527, 527, 527, 527, 527, 527, 527, 528, 528, 528, 528, 528, 528, 528, 528, 529, 529, 529, 529, 529, 529, 529, 529,
        530, 530, 530, 530, 530, 530, 530, 530, 531, 531, 531, 531, 531, 531, 531, 531, 532, 532, 532, 532, 532, 532, 532, 532, 533, 534, 535, 536, 537, 1, 1, 1,
        538, 538, 538, 538, 538, 538, 538, 538, 539, 539, 539, 539, 539, 539, 539, 539, 540, 540, 540, 540, 540, 540, 540, 540, 541, 541, 541, 541, 541, 541, 541, 541,
        542, 542, 542, 542, 542, 542, 542, 542, 543, 543, 543, 543, 543, 543, 543, 543, 544, 544, 544, 544, 544, 544, 544, 544, 545, 546, 547, 548, 549, 1, 1, 1,
        550, 550, 550, 550, 550, 550, 550, 550, 551, 551, 551, 551, 551, 551, 551, 551, 552, 552, 552, 552, 552, 552, 552, 552, 553, 553, 553, 553, 553, 553, 553, 553,
        554, 554, 554, 554, 554, 554, 554, 554, 555, 555, 555, 555, 555, 555, 555, 555, 556, 556, 556, 556, 556, 556, 556, 556, 557, 558, 559, 560, 561, 1, 1, 1,
        562, 562, 562, 562, 562, 562, 562, 562, 563, 563, 563, 563, 563, 563, 563, 563, 564, 564, 564, 564, 564, 564, 564, 564, 565, 565, 565, 565, 565, 565, 565, 565,
        566, 566, 566, 566, 566, 566, 566, 566, 567, 567, 567, 567, 567, 567, 567, 567, 568, 568, 568, 568, 568, 568, 568, 568, 569, 570, 571, 572, 573, 1, 1, 1,
        574, 574, 574, 574, 574, 574, 574, 574, 575, 575, 575, 575, 575, 575, 575, 575, 576, 576, 576, 576, 576, 576, 576, 576, 577, 577, 577, 577, 577, 577, 577, 577,
        578, 578, 578, 578, 578, 578, 578, 578, 579, 579, 579, 579, 579, 579, 579, 579, 580, 580, 580, 580, 580, 580, 580, 580, 581, 582, 583, 584, 585, 1, 1, 1,
        490, 490, 490, 490, 490, 490, 490, 490, 491, 491, 491, 491, 491, 491, 491, 491, 492, 492, 492, 492, 492, 492, 492, 492, 493, 493, 493, 493, 493, 493, 493, 493,
        494, 494, 494, 494, 494, 494, 494, 494, 495, 495, 495, 495, 495, 495, 495, 495, 496, 496, 496, 496, 496, 496, 496, 496, 497, 498, 499, 500, 501, 1, 1, 1,
        502, 502, 502, 502, 502, 502, 502, 502, 503, 503, 503, 503, 503, 503, 503, 503, 504, 504, 504, 504, 504, 504, 504, 504, 505, 505, 505, 505, 505, 505, 505, 505,
        506, 506, 506, 506, 506, 506, 506, 506, 507, 507, 507, 507, 507, 507, 507, 507, 508, 508, 508, 508, 508, 508, 508, 508, 509, 510, 511, 512, 513, 1, 1, 1,
        514, 514, 514, 514, 514, 514, 514, 514, 515, 515, 515, 515, 515, 515, 515, 515, 516, 516, 516, 516, 516, 516, 516, 516, 517, 517, 517, 517, 517, 517, 517, 517,
        518, 518, 518, 518, 518, 518, 518, 518, 519, 519, 519, 519, 519, 519, 519, 519, 520, 520, 520, 520, 520, 520, 520, 520, 521, 522, 523, 524, 525, 1, 1, 1,
        526, 526, 526, 526, 526, 526, 526, 526, 527, 527, 527, 527, 527, 527, 527, 527, 528, 528, 528, 528, 528, 528, 528, 528, 529, 529, 529, 529, 529, 529, 529, 529,
        530, 530, 530, 530, 530, 530, 530, 530, 531, 531, 531, 531, 531, 531, 531, 531, 532, 532, 532, 532, 532, 532, 532, 532, 533, 534, 535, 536, 537, 1, 1, 1,
        538, 538, 538, 538, 538, 538, 538, 538, 539, 539, 539, 539, 539, 539, 539, 539, 540, 540, 540, 540, 540, 540, 540, 540, 541, 541, 541, 541, 541, 541, 541, 541,
        542, 542, 542, 542, 542, 542, 542, 542, 543, 543, 543, 543, 543, 543, 543, 543, 544, 544, 544, 544, 544, 544, 544, 544, 545, 546, 547, 548, 549, 1, 1, 1,
        550, 550, 550, 550, 550, 550, 550, 550, 551, 551, 551, 551, 551, 551, 551, 551, 552, 552, 552, 552, 552, 552, 552, 552, 553, 553, 553, 553, 553, 553, 553, 553,
        554, 554, 554, 554, 554, 554, 554, 554, 555, 555, 555, 555, 555, 555, 555, 555, 556, 556, 556, 556, 556, 556, 556, 556, 557, 558, 559, 560, 561, 1, 1, 1,
        562, 562, 562, 562, 562, 562, 562, 562, 563, 563, 563, 563, 563, 563, 563, 563, 564, 564, 564, 564, 564, 564, 564, 564, 565, 565, 565, 565, 565, 565, 565, 565,
        566, 566, 566, 566, 566, 566, 566, 566, 567, 567, 567, 567, 567, 567, 567, 567, 568, 568, 568, 568, 568, 568, 568, 568, 569, 570, 571, 572, 573, 1, 1, 1,
        586, 586, 586, 586, 586, 586, 586, 586, 587, 587, 587, 587, 587, 587, 587, 587, 588, 588, 588, 588, 588, 588, 588, 588, 589, 589, 589, 589, 589, 589, 589, 589,
        590, 590, 590, 590, 590, 590, 590, 590, 591, 591, 591, 591, 591, 591, 591, 591, 592, 592, 592, 592, 592, 592, 592, 592, 593, 594, 595, 596, 597, 1, 1, 1,
        490, 490, 490, 490, 490, 490, 490, 490, 491, 491, 491, 491, 491, 491, 491, 491, 492, 492, 492, 492, 492, 492, 492, 492, 493, 493, 493, 493, 493, 493, 493, 493,
        494, 494, 494, 494, 494, 494, 494, 494, 495, 495, 495, 495, 495, 495, 495, 495, 496, 496, 496, 496, 496, 496, 496, 496, 497, 498, 499, 500, 501, 1, 1, 1,
        502, 502, 502, 502, 502, 502, 502, 502, 503, 503, 503, 503, 503, 503, 503, 503, 504, 504, 504, 504, 504, 504, 504, 504, 505, 505, 505, 505, 505, 505, 505, 505,
        506, 506, 506, 506, 506, 506, 506, 506, 507, 507, 507, 507, 507, 507, 507, 507, 508, 508, 508, 508, 508, 508, 508, 508, 509, 510, 511, 512, 513, 1, 1, 1,
        514, 514, 514, 514, 514, 514, 514, 514, 515, 515, 515, 515, 515, 515, 515, 515, 516, 516, 516, 516, 516, 516, 516, 516, 517, 517, 517, 517, 517, 517, 517, 517,
        518, 518, 518, 518, 518, 518, 518, 518, 519, 519, 519, 519, 519, 519, 519, 519, 520, 520, 520, 520, 520, 520, 520, 520, 521, 522, 523, 524, 525, 1, 1, 1,
        526, 526, 526, 526, 526, 526, 526, 526, 527, 527, 527, 527, 527, 527, 527, 527, 528, 528, 528, 528, 528, 528, 528, 528, 529, 529, 529, 529, 529, 529, 529, 529,
        530, 530, 530, 530, 530, 530, 530, 530, 531, 531, 531, 531, 531, 531, 531, 531, 532, 532, 532, 532, 532, 532, 532, 532, 533, 534, 535, 536, 537, 1, 1, 1,
        538, 538, 538, 538, 538, 538, 538, 538, 539, 539, 539, 539, 539, 539, 539, 539, 540, 540, 540, 540, 540, 540, 540, 540, 541, 541, 541, 541, 541, 541, 541, 541,
        542, 542, 542, 542, 542, 542, 542, 542, 543, 543, 543, 543, 543, 543, 543, 543, 544, 544, 544, 544, 544, 544, 544, 544, 545, 546, 547, 548, 549, 1, 1, 1,
        550, 550, 550, 550, 550, 550, 550, 550, 551, 551, 551, 551, 551, 551, 551, 551, 552, 552, 552, 552, 552, 552, 552, 552, 553, 553, 553, 553, 553, 553, 553, 553,
        554, 554, 554, 554, 554, 554, 554, 554, 555, 555, 555, 555, 555, 555, 555, 555, 556, 556, 556, 556, 556, 556, 556, 556, 557, 558, 559, 560, 561, 1, 1, 1,
        562, 562, 562, 562, 562, 562, 562, 562, 563, 563, 563, 563, 563, 563, 563, 563, 564, 564, 564, 564, 564, 564, 564, 564, 565, 565, 565, 565, 565, 565, 565, 565,
        566, 566, 566, 566, 566, 566, 566, 566, 567, 567, 567, 567, 567, 567, 567, 567, 568, 568, 568, 568, 568, 568, 568, 568, 569, 570, 571, 572, 573, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        490, 490, 490, 490, 490, 490, 490, 490, 491, 491, 491, 491, 491, 491, 491, 491, 492, 492, 492, 492, 492, 492, 492, 492, 493, 493, 493, 493, 493, 493, 493, 493,
        494, 494, 494, 494, 494, 494, 494, 494, 495, 495, 495, 495, 495, 495, 495, 495, 496, 496, 496, 496, 496, 496, 496, 496, 497, 498, 499, 500, 501, 1, 1, 1,
        502, 502, 502, 502, 502, 502, 502, 502, 503, 503, 503, 503, 503, 503, 503, 503, 504, 504, 504, 504, 504, 504, 504, 504, 505, 505, 505, 505, 505, 505, 505, 505,
        506, 506, 506, 506, 506, 506, 506, 506, 507, 507, 507, 507, 507, 507, 507, 507, 508, 508, 508, 508, 508, 508, 508, 508, 509, 510, 511, 512, 513, 1, 1, 1,
        514, 514, 514, 514, 514, 514, 514, 514, 515, 515, 515, 515, 515, 515, 515, 515, 516, 516, 516, 516, 516, 516, 516, 516, 517, 517, 517, 517, 517, 517, 517, 517,
        518, 518, 518, 518, 518, 518, 518, 518, 519, 519, 519, 519, 519, 519, 519, 519, 520, 520, 520, 520, 520, 520, 520, 520, 521, 522, 523, 524, 525, 1, 1, 1,
        526, 526, 526, 526, 526, 526, 526, 526, 527, 527, 527, 527, 527, 527, 527, 527, 528, 528, 528, 528, 528, 528, 528, 528, 529, 529, 529, 529, 529, 529, 529, 529,
        530, 530, 530, 530, 530, 530, 530, 530, 531, 531, 531, 531, 531, 531, 531, 531, 532, 532, 532, 532, 532, 532, 532, 532, 533, 534, 535, 536, 537, 1, 1, 1,
        538, 538, 538, 538, 538, 538, 538, 538, 539, 539, 539, 539, 539, 539, 539, 539, 540, 540, 540, 540, 540, 540, 540, 540, 541, 541, 541, 541, 541, 541, 541, 541,
        542, 542, 542, 542, 542, 542, 542, 542, 543, 543, 543, 543, 543, 543, 543, 543, 544, 544, 544, 544, 544, 544, 544, 544, 545, 546, 547, 548, 549, 1, 1, 1,
        550, 550, 550, 550, 550, 550, 550, 550, 551, 551, 551, 551, 551, 551, 551, 551, 552, 552, 552, 552, 552, 552, 552, 552, 553, 553, 553, 553, 553, 553, 553, 553,
        554, 554, 554, 554, 554, 554, 554, 554, 555, 555, 555, 555, 555, 555, 555, 555, 556, 556, 556, 556, 556, 556, 556, 556, 557, 558, 559, 560, 561, 1, 1, 1,
        562, 562, 562, 562, 562, 562, 562, 562, 563, 563, 563, 563, 563, 563, 563, 563, 564, 564, 564, 564, 564, 564, 564, 564, 565, 565, 565, 565, 565, 565, 565, 565,
        566, 566, 566, 566, 566, 566, 566, 566, 567, 567, 567, 567, 567, 567, 567, 567, 568, 568, 568, 568, 568, 568, 568, 568, 569, 570, 571, 572, 573, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        490, 490, 490, 490, 490, 490, 490, 490, 491, 491, 491, 491, 491, 491, 491, 491, 492, 492, 492, 492, 492, 492, 492, 492, 493, 493, 493, 493, 493, 493, 493, 493,
        494, 494, 494, 494, 494, 494, 494, 494, 495, 495, 495, 495, 495, 495, 495, 495, 496, 496, 496, 496, 496, 496, 496, 496, 497, 498, 499, 500, 501, 1, 1, 1,
        502, 502, 502, 502, 502, 502, 502, 502, 503, 503, 503, 503, 503, 503, 503, 503, 504, 504, 504, 504, 504, 504, 504, 504, 505, 505, 505, 505, 505, 505, 505, 505,
        506, 506, 506, 506, 506, 506, 506, 506, 507, 507, 507, 507, 507, 507, 507, 507, 508, 508, 508, 508, 508, 508, 508, 508, 509, 510, 511, 512, 513, 1, 1, 1,
        514, 514, 514, 514, 514, 514, 514, 514, 515, 515, 515, 515, 515, 515, 515, 515, 516, 516, 516, 516, 516, 516, 516, 516, 517, 517, 517, 517, 517, 517, 517, 517,
        518, 518, 518, 518, 518, 518, 518, 518, 519, 519, 519, 519, 519, 519, 519, 519, 520, 520, 520, 520, 520, 520, 520, 520, 521, 522, 523, 524, 525, 1, 1, 1,
        526, 526, 526, 526, 526, 526, 526, 526, 527, 527, 527, 527, 527, 527, 527, 527, 528, 528, 528, 528, 528, 528, 528, 528, 529, 529, 529, 529, 529, 529, 529, 529,
        530, 530, 530, 530, 530, 530, 530, 530, 531, 531, 531, 531, 531, 531, 531, 531, 532, 532, 532, 532, 532, 532, 532, 532, 533, 534, 535, 536, 537, 1, 1, 1,
        538, 538, 538, 538, 538, 538, 538, 538, 539, 539, 539, 539, 539, 539, 539, 539, 540, 540, 540, 540, 540, 540, 540, 540, 541, 541, 541, 541, 541, 541, 541, 541,
        542, 542, 542, 542, 542, 542, 542, 542, 543, 543, 543, 543, 543, 543, 543, 543, 544, 544, 544, 544, 544, 544, 544, 544, 545, 546, 547, 548, 549, 1, 1, 1,
        550, 550, 550, 550, 550, 550, 550, 550, 551, 551, 551, 551, 551, 551, 551, 551, 552, 552, 552, 552, 552, 552, 552, 552, 553, 553, 553, 553, 553, 553, 553, 553,
        554, 554, 554, 554, 554, 554, 554, 554, 555, 555, 555, 555, 555, 555, 555, 555, 556, 556, 556, 556, 556, 556, 556, 556, 557, 558, 559, 560, 561, 1, 1, 1,
        562, 562, 562, 562, 562, 562, 562, 562, 563, 563, 563, 563, 563, 563, 563, 563, 564, 564, 564, 564, 564, 564, 564, 564, 565, 565, 565, 565, 565, 565, 565, 565,
        566, 566, 566, 566, 566, 566, 566, 566, 567, 567, 567, 567, 567, 567, 567, 567, 568, 568, 568, 568, 568, 568, 568, 568, 569, 570, 571, 572, 573, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        490, 490, 490, 490, 490, 490, 490, 490, 491, 491, 491, 491, 491, 491, 491, 491, 492, 492, 492, 492, 492, 492, 492, 492, 493, 493, 493, 493, 493, 493, 493, 493,
        494, 494, 494, 494, 494, 494, 494, 494, 495, 495, 495, 495, 495, 495, 495, 495, 496, 496, 496, 496, 496, 496, 496, 496, 497, 498, 499, 500, 501, 1, 1, 1,
        502, 502, 502, 502, 502, 502, 502, 502, 503, 503, 503, 503, 503, 503, 503, 503, 504, 504, 504, 504, 504, 504, 504, 504, 505, 505, 505, 505, 505, 505, 505, 505,
        506, 506, 506, 506, 506, 506, 506, 506, 507, 507, 507, 507, 507, 507, 507, 507, 508, 508, 508, 508, 508, 508, 508, 508, 509, 510, 511, 512, 513, 1, 1, 1,
        514, 514, 514, 514, 514, 514, 514, 514, 515, 515, 515, 515, 515, 515, 515, 515, 516, 516, 516, 516, 516, 516, 516, 516, 517, 517, 517, 517, 517, 517, 517, 517,
        518, 518, 518, 518, 518, 518, 518, 518, 519, 519, 519, 519, 519, 519, 519, 519, 520, 520, 520, 520, 520, 520, 520, 520, 521, 522, 523, 524, 525, 1, 1, 1,
        526, 526, 526, 526, 526, 526, 526, 526, 527, 527, 527, 527, 527, 527, 527, 527, 528, 528, 528, 528, 528, 528, 528, 528, 529, 529, 529, 529, 529, 529, 529, 529,
        530, 530, 530, 530, 530, 530, 530, 530, 531, 531, 531, 531, 531, 531, 531, 531, 532, 532, 532, 532, 532, 532, 532, 532, 533, 534, 535, 536, 537, 1, 1, 1,
        538, 538, 538, 538, 538, 538, 538, 538, 539, 539, 539, 539, 539, 539, 539, 539, 540, 540, 540, 540, 540, 540, 540, 540, 541, 541, 541, 541, 541, 541, 541, 541,
        542, 542, 542, 542, 542, 542, 542, 542, 543, 543, 543, 543, 543, 543, 543, 543, 544, 544, 544, 544, 544, 544, 544, 544, 545, 546, 547, 548, 549, 1, 1, 1,
        550, 550, 550, 550, 550, 550, 550, 550, 551, 551, 551, 551, 551, 551, 551, 551, 552, 552, 552, 552, 552, 552, 552, 552, 553, 553, 553, 553, 553, 553, 553, 553,
        554, 554, 554, 554, 554, 554, 554, 554, 555, 555, 555, 555, 555, 555, 555, 555, 556, 556, 556, 556, 556, 556, 556, 556, 557, 558, 559, 560, 561, 1, 1, 1,
        562, 562, 562, 562, 562, 562, 562, 562, 563, 563, 563, 563, 563, 563, 563, 563, 564, 564, 564, 564, 564, 564, 564, 564, 565, 565, 565, 565, 565, 565, 565, 565,
        566, 566, 566, 566, 566, 566, 566, 566, 567, 567, 567, 567, 567, 567, 567, 567, 568, 568, 568, 568, 568, 568, 568, 568, 569, 570, 571, 572, 573, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        490, 490, 490, 490, 490, 490, 490, 490, 491, 491, 491, 491, 491, 491, 491, 491, 492, 492, 492, 492, 492, 492, 492, 492, 493, 493, 493, 493, 493, 493, 493, 493,
        494, 494, 494, 494, 494, 494, 494, 494, 495, 495, 495, 495, 495, 495, 495, 495, 496, 496, 496, 496, 496, 496, 496, 496, 497, 498, 499, 500, 501, 1, 1, 1,
        502, 502, 502, 502, 502, 502, 502, 502, 503, 503, 503, 503, 503, 503, 503, 503, 504, 504, 504, 504, 504, 504, 504, 504, 505, 505, 505, 505, 505, 505, 505, 505,
        506, 506, 506, 506, 506, 506, 506, 506, 507, 507, 507, 507, 507, 507, 507, 507, 508, 508, 508, 508, 508, 508, 508, 508, 509, 510, 511, 512, 513, 1, 1, 1,
        514, 514, 514, 514, 514, 514, 514, 514, 515, 515, 515, 515, 515, 515, 515, 515, 516, 516, 516, 516, 516, 516, 516, 516, 517, 517, 517, 517, 517, 517, 517, 517,
        518, 518, 518, 518, 518, 518, 518, 518, 519, 519, 519, 519, 519, 519, 519, 519, 520, 520, 520, 520, 520, 520, 520, 520, 521, 522, 523, 524, 525, 1, 1, 1,
        526, 526, 526, 526, 526, 526, 526, 526, 527, 527, 527, 527, 527, 527, 527, 527, 528, 528, 528, 528, 528, 528, 528, 528, 529, 529, 529, 529, 529, 529, 529, 529,
        530, 530, 530, 530, 530, 530, 530, 530, 531, 531, 531, 531, 531, 531, 531, 531, 532, 532, 532, 532, 532, 532, 532, 532, 533, 534, 535, 536, 537, 1, 1, 1,
        538, 538, 538, 538, 538, 538, 538, 538, 539, 539, 539, 539, 539, 539, 539, 539, 540, 540, 540, 540, 540, 540, 540, 540, 541, 541, 541, 541, 541, 541, 541, 541,
        542, 542, 542, 542, 542, 542, 542, 542, 543, 543, 543, 543, 543, 543, 543, 543, 544, 544, 544, 544, 544, 544, 544, 544, 545, 546, 547, 548, 549, 1, 1, 1,
        550, 550, 550, 550, 550, 550, 550, 550, 551, 551, 551, 551, 551, 551, 551, 551, 552, 552, 552, 552, 552, 552, 552, 552, 553, 553, 553, 553, 553, 553, 553, 553,
        554, 554, 554, 554, 554, 554, 554, 554, 555, 555, 555, 555, 555, 555, 555, 555, 556, 556, 556, 556, 556, 556, 556, 556, 557, 558, 559, 560, 561, 1, 1, 1,
        562, 562, 562, 562, 562, 562, 562, 562, 563, 563, 563, 563, 563, 563, 563, 563, 564, 564, 564, 564, 564, 564, 564, 564, 565, 565, 565, 565, 565, 565, 565, 565,
        566, 566, 566, 566, 566, 566, 566, 566, 567, 567, 567, 567, 567, 567, 567, 567, 568, 568, 568, 568, 568, 568, 568, 568, 569, 570, 571, 572, 573, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        490, 490, 490, 490, 490, 490, 490, 490, 491, 491, 491, 491, 491, 491, 491, 491, 492, 492, 492, 492, 492, 492, 492, 492, 493, 493, 493, 493, 493, 493, 493, 493,
        494, 494, 494, 494, 494, 494, 494, 494, 495, 495, 495, 495, 495, 495, 495, 495, 496, 496, 496, 496, 496, 496, 496, 496, 497, 498, 499, 500, 501, 1, 1, 1,
        502, 502, 502, 502, 502, 502, 502, 502, 503, 503, 503, 503, 503, 503, 503, 503, 504, 504, 504, 504, 504, 504, 504, 504, 505, 505, 505, 505, 505, 505, 505, 505,
        506, 506, 506, 506, 506, 506, 506, 506, 507, 507, 507, 507, 507, 507, 507, 507, 508, 508, 508, 508, 508, 508, 508, 508, 509, 510, 511, 512, 513, 1, 1, 1,
        514, 514, 514, 514, 514, 514, 514, 514, 515, 515, 515, 515, 515, 515, 515, 515, 516, 516, 516, 516, 516, 516, 516, 516, 517, 517, 517, 517, 517, 517, 517, 517,
        518, 518, 518, 518, 518, 518, 518, 518, 519, 519, 519, 519, 519, 519, 519, 519, 520, 520, 520, 520, 520, 520, 520, 520, 521, 522, 523, 524, 525, 1, 1, 1,
        526, 526, 526, 526, 526, 526, 526, 526, 527, 527, 527, 527, 527, 527, 527, 527, 528, 528, 528, 528, 528, 528, 528, 528, 529, 529, 529, 529, 529, 529, 529, 529,
        530, 530, 530, 530, 530, 530, 530, 530, 531, 531, 531, 531, 531, 531, 531, 531, 532, 532, 532, 532, 532, 532, 532, 532, 533, 534, 535, 536, 537, 1, 1, 1,
        538, 538, 538, 538, 538, 538, 538, 538, 539, 539, 539, 539, 539, 539, 539, 539, 540, 540, 540, 540, 540, 540, 540, 540, 541, 541, 541, 541, 541, 541, 541, 541,
        542, 542, 542, 542, 542, 542, 542, 542, 543, 543, 543, 543, 543, 543, 543, 543, 544, 544, 544, 544, 544, 544, 544, 544, 545, 546, 547, 548, 549, 1, 1, 1,
        550, 550, 550, 550, 550, 550, 550, 550, 551, 551, 551, 551, 551, 551, 551, 551, 552, 552, 552, 552, 552, 552, 552, 552, 553, 553, 553, 553, 553, 553, 553, 553,
        554, 554, 554, 554, 554, 554, 554, 554, 555, 555, 555, 555, 555, 555, 555, 555, 556, 556, 556, 556, 556, 556, 556, 556, 557, 558, 559, 560, 561, 1, 1, 1,
        562, 562, 562, 562, 562, 562, 562, 562, 563, 563, 563, 563, 563, 563, 563, 563, 564, 564, 564, 564, 564, 564, 564, 564, 565, 565, 565, 565, 565, 565, 565, 565,
        566, 566, 566, 566, 566, 566, 566, 566, 567, 567, 567, 567, 567, 567, 567, 567, 568, 568, 568, 568, 568, 568, 568, 568, 569, 570, 571, 572, 573, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        598, 598, 598, 598, 598, 598, 598, 598, 1, 1, 1, 1, 1, 1, 1, 1, 599, 599, 599, 599, 599, 599, 599, 599, 600, 600, 600, 600, 600, 600, 600, 601,
        602, 602, 602, 602, 602, 602, 602, 603, 604, 604, 604, 604, 604, 604, 604, 604, 605, 605, 605, 605, 605, 605, 605, 605, 606, 607, 1, 1, 1, 1, 1, 1,
        608, 608, 608, 608, 608, 608, 608, 608, 1, 1, 1, 1, 1, 1, 1, 1, 609, 609, 609, 609, 609, 609, 609, 609, 610, 610, 610, 610, 610, 610, 610, 610,
        611, 611, 611, 611, 611, 611, 611, 611, 612, 612, 612, 612, 612, 612, 612, 612, 613, 613, 613, 613, 613, 613, 613, 613, 614, 615, 1, 1, 1, 1, 1, 1,
        616, 616, 616, 616, 616, 616, 616, 616, 1, 1, 1, 1, 1, 1, 1, 1, 617, 617, 617, 617, 617, 617, 617, 617, 618, 618, 618, 618, 618, 618, 618, 618,
        619, 619, 619, 619, 619, 619, 619, 619, 620, 620, 620, 620, 620, 620, 620, 620, 621, 621, 621, 621, 621, 621, 621, 621, 622, 623, 1, 1, 1, 1, 1, 1,
        624, 624, 624, 624, 624, 624, 624, 624, 1, 1, 1, 1, 1, 1, 1, 1, 625, 625, 625, 625, 625, 625, 625, 625, 626, 626, 626, 626, 626, 626, 626, 626,
        627, 627, 627, 627, 627, 627, 627, 627, 628, 628, 628, 628, 628, 628, 628, 628, 629, 629, 629, 629, 629, 629, 629, 629, 630, 631, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        632, 632, 632, 632, 632, 632, 632, 632, 1, 1, 1, 1, 1, 1, 1, 1, 633, 633, 633, 633, 633, 633, 633, 633, 634, 634, 634, 634, 634, 634, 634, 634,
        635, 635, 635, 635, 635, 635, 635, 635, 636, 636, 636, 636, 636, 636, 636, 636, 637, 637, 637, 637, 637, 637, 637, 637, 638, 639, 640, 641, 642, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 643, 643, 643, 643, 643, 643, 643, 643, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 644, 644, 644, 644, 644, 644, 644, 644, 645, 645, 645, 645, 645, 645, 645, 645, 646, 647, 648, 649, 1, 1, 1, 1,
        650, 650, 650, 650, 650, 650, 650, 650, 1, 1, 1, 1, 1, 1, 1, 1, 651, 651, 651, 651, 651, 651, 651, 651, 652, 652, 652, 652, 652, 652, 652, 653,
        654, 654, 654, 654, 654, 654, 654, 655, 656, 656, 656, 656, 656, 656, 656, 656, 657, 657, 657, 657, 657, 657, 657, 657, 658, 659, 1, 1, 1, 1, 1, 1,
        660, 660, 660, 660, 660, 660, 660, 660, 1, 1, 1, 1, 1, 1, 1, 1, 661, 661, 661, 661, 661, 661, 661, 661, 662, 662, 662, 662, 662, 662, 662, 662,
        663, 663, 663, 663, 663, 663, 663, 663, 664, 664, 664, 664, 664, 664, 664, 664, 665, 665, 665, 665, 665, 665, 665, 665, 666, 667, 1, 1, 1, 1, 1, 1,
        668, 668, 668, 668, 668, 668, 668, 668, 1, 1, 1, 1, 1, 1, 1, 1, 669, 669, 669, 669, 669, 669, 669, 669, 670, 670, 670, 670, 670, 670, 670, 670,
        671, 671, 671, 671, 671, 671, 671, 671, 672, 672, 672, 672, 672, 672, 672, 672, 673, 673, 673, 673, 673, 673, 673, 673, 674, 675, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        632, 632, 632, 632, 632, 632, 632, 632, 1, 1, 1, 1, 1, 1, 1, 1, 633, 633, 633, 633, 633, 633, 633, 633, 634, 634, 634, 634, 634, 634, 634, 634,
        635, 635, 635, 635, 635, 635, 635, 635, 636, 636, 636, 636, 636, 636, 636, 636, 637, 637, 637, 637, 637, 637, 637, 637, 638, 639, 640, 641, 642, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 643, 643, 643, 643, 643, 643, 643, 643, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 644, 644, 644, 644, 644, 644, 644, 644, 645, 645, 645, 645, 645, 645, 645, 645, 646, 647, 648, 649, 1, 1, 1, 1,
        676, 676, 676, 676, 676, 676, 676, 676, 1, 1, 1, 1, 1, 1, 1, 1, 677, 677, 677, 677, 677, 677, 677, 677, 678, 678, 678, 678, 678, 678, 678, 679,
        680, 680, 680, 680, 680, 680, 680, 681, 682, 682, 682, 682, 682, 682, 682, 682, 683, 683, 683, 683, 683, 683, 683, 683, 684, 685, 1, 1, 1, 1, 1, 1,
        686, 686, 686, 686, 686, 686, 686, 686, 1, 1, 1, 1, 1, 1, 1, 1, 687, 687, 687, 687, 687, 687, 687, 687, 688, 688, 688, 688, 688, 688, 688, 688,
        689, 689, 689, 689, 689, 689, 689, 689, 690, 690, 690, 690, 690, 690, 690, 690, 691, 691, 691, 691, 691, 691, 691, 691, 692, 693, 1, 1, 1, 1, 1, 1,
        694, 694, 694, 694, 694, 694, 694, 694, 1, 1, 1, 1, 1, 1, 1, 1, 695, 695, 695, 695, 695, 695, 695, 695, 696, 696, 696, 696, 696, 696, 696, 696,
        697, 697, 697, 697, 697, 697, 697, 697, 698, 698, 698, 698, 698, 698, 698, 698, 699, 699, 699, 699, 699, 699, 699, 699, 700, 701, 1, 1, 1, 1, 1, 1,
        702, 702, 702, 702, 702, 702, 702, 702, 1, 1, 1, 1, 1, 1, 1, 1, 703, 703, 703, 703, 703, 703, 703, 703, 704, 704, 704, 704, 704, 704, 704, 704,
        705, 705, 705, 705, 705, 705, 705, 705, 706, 706, 706, 706, 706, 706, 706, 706, 707, 707, 707, 707, 707, 707, 707, 707, 708, 709, 710, 711, 712, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        632, 632, 632, 632, 632, 632, 632, 632, 1, 1, 1, 1, 1, 1, 1, 1, 633, 633, 633, 633, 633, 633, 633, 633, 634, 634, 634, 634, 634, 634, 634, 634,
        635, 635, 635, 635, 635, 635, 635, 635, 636, 636, 636, 636, 636, 636, 636, 636, 637, 637, 637, 637, 637, 637, 637, 637, 638, 639, 640, 641, 642, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 643, 643, 643, 643, 643, 643, 643, 643, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 644, 644, 644, 644, 644, 644, 644, 644, 645, 645, 645, 645, 645, 645, 645, 645, 646, 647, 648, 649, 1, 1, 1, 1,
        713, 713, 713, 713, 713, 713, 713, 713, 1, 1, 1, 1, 1, 1, 1, 1, 714, 714, 714, 714, 714, 714, 714, 714, 715, 715, 715, 715, 715, 715, 715, 716,
        717, 717, 717, 717, 717, 717, 717, 718, 719, 719, 719, 719, 719, 719, 719, 719, 720, 720, 720, 720, 720, 720, 720, 720, 721, 722, 1, 1, 1, 1, 1, 1,
        723, 723, 723, 723, 723, 723, 723, 723, 1, 1, 1, 1, 1, 1, 1, 1, 724, 724, 724, 724, 724, 724, 724, 724, 725, 725, 725, 725, 725, 725, 725, 725,
        726, 726, 726, 726, 726, 726, 726, 726, 727, 727, 727, 727, 727, 727, 727, 727, 728, 728, 728, 728, 728, 728, 728, 728, 729, 730, 1, 1, 1, 1, 1, 1,
        731, 731, 731, 731, 731, 731, 731, 731, 1, 1, 1, 1, 1, 1, 1, 1, 732, 732, 732, 732, 732, 732, 732, 732, 733, 733, 733, 733, 733, 733, 733, 733,
        734, 734, 734, 734, 734, 734, 734, 734, 735, 735, 735, 735, 735, 735, 735, 735, 736, 736, 736, 736, 736, 736, 736, 736, 737, 738, 1, 1, 1, 1, 1, 1,
        739, 739, 739, 739, 739, 739, 739, 739, 1, 1, 1, 1, 1, 1, 1, 1, 740, 740, 740, 740, 740, 740, 740, 740, 741, 741, 741, 741, 741, 741, 741, 741,
        742, 742, 742, 742, 742, 742, 742, 742, 743, 743, 743, 743, 743, 743, 743, 743, 744, 744, 744, 744, 744, 744, 744, 744, 745, 746, 747, 748, 749, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        632, 632, 632, 632, 632, 632, 632, 632, 1, 1, 1, 1, 1, 1, 1, 1, 633, 633, 633, 633, 633, 633, 633, 633, 634, 634, 634, 634, 634, 634, 634, 634,
        635, 635, 635, 635, 635, 635, 635, 635, 636, 636, 636, 636, 636, 636, 636, 636, 637, 637, 637, 637, 637, 637, 637, 637, 638, 639, 640, 641, 642, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 643, 643, 643, 643, 643, 643, 643, 643, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 644, 644, 644, 644, 644, 644, 644, 644, 645, 645, 645, 645, 645, 645, 645, 645, 646, 647, 648, 649, 1, 1, 1, 1,
        750, 750, 750, 750, 750, 750, 750, 750, 1, 1, 1, 1, 1, 1, 1, 1, 751, 751, 751, 751, 751, 751, 751, 751, 752, 752, 752, 752, 752, 752, 752, 753,
        754, 754, 754, 754, 754, 754, 754, 755, 756, 756, 756, 756, 756, 756, 756, 756, 757, 757, 757, 757, 757, 757, 757, 757, 758, 759, 1, 1, 1, 1, 1, 1,
        760, 760, 760, 760, 760, 760, 760, 760, 1, 1, 1, 1, 1, 1, 1, 1, 761, 761, 761, 761, 761, 761, 761, 761, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 762, 762, 762, 762, 762, 762, 762, 762, 763, 763, 763, 763, 763, 763, 763, 763, 764, 765, 766, 767, 1, 1, 1, 1,
        768, 768, 768, 768, 768, 768, 768, 768, 1, 1, 1, 1, 1, 1, 1, 1, 769, 769, 769, 769, 769, 769, 769, 769, 1, 1, 1, 1, 1, 1, 1, 1,
        770, 770, 770, 770, 770, 770, 770, 770, 771, 771, 771, 771, 771, 771, 771, 771, 772, 772, 772, 772, 772, 772, 772, 772, 773, 774, 1, 1, 1, 1, 1, 1,
        775, 775, 775, 775, 775, 775, 775, 775, 1, 1, 1, 1, 1, 1, 1, 1, 776, 776, 776, 776, 776, 776, 776, 776, 1, 1, 1, 1, 1, 1, 1, 1,
        777, 777, 777, 777, 777, 777, 777, 777, 778, 778, 778, 778, 778, 778, 778, 778, 779, 779, 779, 779, 779, 779, 779, 779, 780, 781, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        632, 632, 632, 632, 632, 632, 632, 632, 1, 1, 1, 1, 1, 1, 1, 1, 633, 633, 633, 633, 633, 633, 633, 633, 634, 634, 634, 634, 634, 634, 634, 634,
        635, 635, 635, 635, 635, 635, 635, 635, 636, 636, 636, 636, 636, 636, 636, 636, 637, 637, 637, 637, 637, 637, 637, 637, 638, 639, 640, 641, 642, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 643, 643, 643, 643, 643, 643, 643, 643, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 644, 644, 644, 644, 644, 644, 644, 644, 645, 645, 645, 645, 645, 645, 645, 645, 646, 647, 648, 649, 1, 1, 1, 1,
        782, 782, 782, 782, 782, 782, 782, 782, 1, 1, 1, 1, 1, 1, 1, 1, 783, 783, 783, 783, 783, 783, 783, 783, 784, 784, 784, 784, 784, 784, 784, 785,
        786, 786, 786, 786, 786, 786, 786, 787, 788, 788, 788, 788, 788, 788, 788, 788, 789, 789, 789, 789, 789, 789, 789, 789, 790, 791, 1, 1, 1, 1, 1, 1,
        792, 792, 792, 792, 792, 792, 792, 792, 1, 1, 1, 1, 1, 1, 1, 1, 793, 793, 793, 793, 793, 793, 793, 793, 794, 794, 794, 794, 794, 794, 794, 794,
        795, 795, 795, 795, 795, 795, 795, 795, 796, 796, 796, 796, 796, 796, 796, 796, 797, 797, 797, 797, 797, 797, 797, 797, 798, 799, 1, 1, 1, 1, 1, 1,
        800, 800, 800, 800, 800, 800, 800, 800, 1, 1, 1, 1, 1, 1, 1, 1, 801, 801, 801, 801, 801, 801, 801, 801, 802, 802, 802, 802, 802, 802, 802, 802,
        803, 803, 803, 803, 803, 803, 803, 803, 804, 804, 804, 804, 804, 804, 804, 804, 805, 805, 805, 805, 805, 805, 805, 805, 806, 807, 1, 1, 1, 1, 1, 1,
        808, 808, 808, 808, 808, 808, 808, 808, 1, 1, 1, 1, 1, 1, 1, 1, 809, 809, 809, 809, 809, 809, 809, 809, 810, 810, 810, 810, 810, 810, 810, 811,
        812, 812, 812, 812, 812, 812, 812, 813, 814, 814, 814, 814, 814, 814, 814, 814, 815, 815, 815, 815, 815, 815, 815, 815, 816, 817, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        632, 632, 632, 632, 632, 632, 632, 632, 1, 1, 1, 1, 1, 1, 1, 1, 633, 633, 633, 633, 633, 633, 633, 633, 634, 634, 634, 634, 634, 634, 634, 634,
        635, 635, 635, 635, 635, 635, 635, 635, 636, 636, 636, 636, 636, 636, 636, 636, 637, 637, 637, 637, 637, 637, 637, 637, 638, 639, 640, 641, 642, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 643, 643, 643, 643, 643, 643, 643, 643, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 644, 644, 644, 644, 644, 644, 644, 644, 645, 645, 645, 645, 645, 645, 645, 645, 646, 647, 648, 649, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 818, 818, 818, 818, 818, 818, 818, 818, 819, 819, 819, 819, 819, 819, 819, 819,
        1, 1, 1, 1, 1, 1, 1, 1, 820, 820, 820, 820, 820, 820, 820, 820, 821, 821, 821, 821, 821, 821, 821, 821, 822, 823, 824, 825, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 826, 826, 826, 826, 826, 826, 826, 826, 827, 827, 827, 827, 827, 827, 827, 827,
        1, 1, 1, 1, 1, 1, 1, 1, 828, 828, 828, 828, 828, 828, 828, 828, 829, 829, 829, 829, 829, 829, 829, 829, 830, 831, 832, 833, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        632, 632, 632, 632, 632, 632, 632, 632, 1, 1, 1, 1, 1, 1, 1, 1, 633, 633, 633, 633, 633, 633, 633, 633, 634, 634, 634, 634, 634, 634, 634, 634,
        635, 635, 635, 635, 635, 635, 635, 635, 636, 636, 636, 636, 636, 636, 636, 636, 637, 637, 637, 637, 637, 637, 637, 637, 638, 639, 640, 641, 642, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 643, 643, 643, 643, 643, 643, 643, 643, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 644, 644, 644, 644, 644, 644, 644, 644, 645, 645, 645, 645, 645, 645, 645, 645, 646, 647, 648, 649, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        834, 834, 834, 834, 834, 834, 834, 834, 834, 834, 834, 834, 834, 834, 834, 834, 835, 835, 835, 835, 835, 835, 835, 836, 837, 837, 837, 837, 837, 837, 837, 838,
        839, 839, 839, 839, 839, 839, 839, 839, 840, 840, 840, 840, 840, 840, 840, 840, 841, 842, 843, 844, 1, 845, 846, 847, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 848, 848, 848, 848, 848, 848, 848, 848, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 849, 849, 849, 849, 849, 849, 849, 849, 850, 850, 850, 850, 850, 850, 850, 850, 851, 852, 853, 854, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 855, 855, 855, 855, 855, 855, 855, 855, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 856, 856, 856, 856, 856, 856, 856, 856, 857, 857, 857, 857, 857, 857, 857, 857, 858, 859, 860, 861, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        632, 632, 632, 632, 632, 632, 632, 632, 1, 1, 1, 1, 1, 1, 1, 1, 633, 633, 633, 633, 633, 633, 633, 633, 634, 634, 634, 634, 634, 634, 634, 634,
        635, 635, 635, 635, 635, 635, 635, 635, 636, 636, 636, 636, 636, 636, 636, 636, 637, 637, 637, 637, 637, 637, 637, 637, 638, 639, 640, 641, 642, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 643, 643, 643, 643, 643, 643, 643, 643, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 644, 644, 644, 644, 644, 644, 644, 644, 645, 645, 645, 645, 645, 645, 645, 645, 646, 647, 648, 649, 1, 1, 1, 1,
        862, 862, 862, 862, 862, 862, 862, 862, 1, 1, 1, 1, 1, 1, 1, 1, 863, 863, 863, 863, 863, 863, 863, 863, 864, 864, 864, 864, 864, 864, 864, 865,
        866, 866, 866, 866, 866, 866, 866, 867, 868, 868, 868, 868, 868, 868, 868, 868, 869, 869, 869, 869, 869, 869, 869, 869, 870, 871, 1, 1, 1, 1, 1, 1,
        872, 872, 872, 872, 872, 872, 872, 872, 873, 873, 873, 873, 873, 873, 873, 873, 874, 874, 874, 874, 874, 874, 874, 874, 875, 875, 875, 875, 875, 875, 875, 875,
        876, 876, 876, 876, 876, 876, 876, 876, 877, 877, 877, 877, 877, 877, 877, 877, 878, 878, 878, 878, 878, 878, 878, 878, 879, 880, 1, 1, 1, 1, 1, 1,
        881, 881, 881, 881, 881, 881, 881, 881, 882, 882, 882, 882, 882, 882, 882, 882, 883, 883, 883, 883, 883, 883, 883, 883, 884, 884, 884, 884, 884, 884, 884, 884,
        885, 885, 885, 885, 885, 885, 885, 885, 886, 886, 886, 886, 886, 886, 886, 886, 887, 887, 887, 887, 887, 887, 887, 887, 888, 889, 1, 1, 1, 1, 1, 1,
        890, 890, 890, 890, 890, 890, 890, 890, 891, 891, 891, 891, 891, 891, 891, 891, 892, 892, 892, 892, 892, 892, 892, 892, 893, 893, 893, 893, 893, 893, 893, 894,
        895, 895, 895, 895, 895, 895, 895, 896, 897, 897, 897, 897, 897, 897, 897, 897, 898, 898, 898, 898, 898, 898, 898, 898, 899, 900, 1, 1, 1, 1, 1, 1,
        901, 901, 901, 901, 901, 901, 901, 901, 1, 1, 1, 1, 1, 1, 1, 1, 902, 902, 902, 902, 902, 902, 902, 902, 903, 903, 903, 903, 903, 903, 903, 904,
        905, 905, 905, 905, 905, 905, 905, 906, 907, 907, 907, 907, 907, 907, 907, 907, 908, 908, 908, 908, 908, 908, 908, 908, 909, 910, 1, 1, 1, 1, 1, 1,
        911, 911, 911, 911, 911, 911, 911, 911, 912, 912, 912, 912, 912, 912, 912, 912, 913, 913, 913, 913, 913, 913, 913, 913, 914, 914, 914, 914, 914, 914, 914, 914,
        915, 915, 915, 915, 915, 915, 915, 915, 916, 916, 916, 916, 916, 916, 916, 916, 917, 917, 917, 917, 917, 917, 917, 917, 918, 919, 1, 1, 1, 1, 1, 1,
        920, 920, 920, 920, 920, 920, 920, 920, 921, 921, 921, 921, 921, 921, 921, 921, 922, 922, 922, 922, 922, 922, 922, 922, 923, 923, 923, 923, 923, 923, 923, 923,
        924, 924, 924, 924, 924, 924, 924, 924, 925, 925, 925, 925, 925, 925, 925, 925, 926, 926, 926, 926, 926, 926, 926, 926, 927, 928, 1, 1, 1, 1, 1, 1,
        929, 929, 929, 929, 929, 929, 929, 929, 930, 930, 930, 930, 930, 930, 930, 930, 931, 931, 931, 931, 931, 931, 931, 931, 932, 932, 932, 932, 932, 932, 932, 933,
        934, 934, 934, 934, 934, 934, 934, 935, 936, 936, 936, 936, 936, 936, 936, 936, 937, 937, 937, 937, 937, 937, 937, 937, 938, 939, 1, 1, 1, 1, 1, 1,
        862, 862, 862, 862, 862, 862, 862, 862, 1, 1, 1, 1, 1, 1, 1, 1, 863, 863, 863, 863, 863, 863, 863, 863, 864, 864, 864, 864, 864, 864, 864, 865,
        866, 866, 866, 866, 866, 866, 866, 867, 868, 868, 868, 868, 868, 868, 868, 868, 869, 869, 869, 869, 869, 869, 869, 869, 870, 871, 1, 1, 1, 1, 1, 1,
        872, 872, 872, 872, 872, 872, 872, 872, 873, 873, 873, 873, 873, 873, 873, 873, 874, 874, 874, 874, 874, 874, 874, 874, 875, 875, 875, 875, 875, 875, 875, 875,
        876, 876, 876, 876, 876, 876, 876, 876, 877, 877, 877, 877, 877, 877, 877, 877, 878, 878, 878, 878, 878, 878, 878, 878, 879, 880, 1, 1, 1, 1, 1, 1,
        881, 881, 881, 881, 881, 881, 881, 881, 882, 882, 882, 882, 882, 882, 882, 882, 883, 883, 883, 883, 883, 883, 883, 883, 884, 884, 884, 884, 884, 884, 884, 884,
        885, 885, 885, 885, 885, 885, 885, 885, 886, 886, 886, 886, 886, 886, 886, 886, 887, 887, 887, 887, 887, 887, 887, 887, 888, 889, 1, 1, 1, 1, 1, 1,
        940, 940, 940, 940, 940, 940, 940, 940, 941, 941, 941, 941, 941, 941, 941, 941, 942, 942, 942, 942, 942, 942, 942, 942, 943, 943, 943, 943, 943, 943, 943, 944,
        945, 945, 945, 945, 945, 945, 945, 946, 947, 947, 947, 947, 947, 947, 947, 947, 948, 948, 948, 948, 948, 948, 948, 948, 949, 950, 1, 1, 1, 1, 1, 1,
        901, 901, 901, 901, 901, 901, 901, 901, 1, 1, 1, 1, 1, 1, 1, 1, 902, 902, 902, 902, 902, 902, 902, 902, 903, 903, 903, 903, 903, 903, 903, 904,
        905, 905, 905, 905, 905, 905, 905, 906, 907, 907, 907, 907, 907, 907, 907, 907, 908, 908, 908, 908, 908, 908, 908, 908, 909, 910, 1, 1, 1, 1, 1, 1,
        911, 911, 911, 911, 911, 911, 911, 911, 912, 912, 912, 912, 912, 912, 912, 912, 913, 913, 913, 913, 913, 913, 913, 913, 914, 914, 914, 914, 914, 914, 914, 914,
        915, 915, 915, 915, 915, 915, 915, 915, 916, 916, 916, 916, 916, 916, 916, 916, 917, 917, 917, 917, 917, 917, 917, 917, 918, 919, 1, 1, 1, 1, 1, 1,
        920, 920, 920, 920, 920, 920, 920, 920, 921, 921, 921, 921, 921, 921, 921, 921, 922, 922, 922, 922, 922, 922, 922, 922, 923, 923, 923, 923, 923, 923, 923, 923,
        924, 924, 924, 924, 924, 924, 924, 924, 925, 925, 925, 925, 925, 925, 925, 925, 926, 926, 926, 926, 926, 926, 926, 926, 927, 928, 1, 1, 1, 1, 1, 1,
        951, 951, 951, 951, 951, 951, 951, 951, 952, 952, 952, 952, 952, 952, 952, 952, 953, 953, 953, 953, 953, 953, 953, 953, 954, 954, 954, 954, 954, 954, 954, 955,
        956, 956, 956, 956, 956, 956, 956, 957, 958, 958, 958, 958, 958, 958, 958, 958, 959, 959, 959, 959, 959, 959, 959, 959, 960, 961, 1, 1, 1, 1, 1, 1,
        862, 862, 862, 862, 862, 862, 862, 862, 1, 1, 1, 1, 1, 1, 1, 1, 863, 863, 863, 863, 863, 863, 863, 863, 864, 864, 864, 864, 864, 864, 864, 865,
        866, 866, 866, 866, 866, 866, 866, 867, 868, 868, 868, 868, 868, 868, 868, 868, 869, 869, 869, 869, 869, 869, 869, 869, 870, 871, 1, 1, 1, 1, 1, 1,
        872, 872, 872, 872, 872, 872, 872, 872, 873, 873, 873, 873, 873, 873, 873, 873, 874, 874, 874, 874, 874, 874, 874, 874, 875, 875, 875, 875, 875, 875, 875, 875,
        876, 876, 876, 876, 876, 876, 876, 876, 877, 877, 877, 877, 877, 877, 877, 877, 878, 878, 878, 878, 878, 878, 878, 878, 879, 880, 1, 1, 1, 1, 1, 1,
        881, 881, 881, 881, 881, 881, 881, 881, 882, 882, 882, 882, 882, 882, 882, 882, 883, 883, 883, 883, 883, 883, 883, 883, 884, 884, 884, 884, 884, 884, 884, 884,
        885, 885, 885, 885, 885, 885, 885, 885, 886, 886, 886, 886, 886, 886, 886, 886, 887, 887, 887, 887, 887, 887, 887, 887, 888, 889, 1, 1, 1, 1, 1, 1,
        962, 962, 962, 962, 962, 962, 962, 962, 963, 963, 963, 963, 963, 963, 963, 963, 964, 964, 964, 964, 964, 964, 964, 964, 965, 965, 965, 965, 965, 965, 965, 966,
        967, 967, 967, 967, 967, 967, 967, 968, 969, 969, 969, 969, 969, 969, 969, 969, 970, 970, 970, 970, 970, 970, 970, 970, 971, 972, 1, 1, 1, 1, 1, 1,
        901, 901, 901, 901, 901, 901, 901, 901, 1, 1, 1, 1, 1, 1, 1, 1, 902, 902, 902, 902, 902, 902, 902, 902, 903, 903, 903, 903, 903, 903, 903, 904,
        905, 905, 905, 905, 905, 905, 905, 906, 907, 907, 907, 907, 907, 907, 907, 907, 908, 908, 908, 908, 908, 908, 908, 908, 909, 910, 1, 1, 1, 1, 1, 1,
        911, 911, 911, 911, 911, 911, 911, 911, 912, 912, 912, 912, 912, 912, 912, 912, 913, 913, 913, 913, 913, 913, 913, 913, 914, 914, 914, 914, 914, 914, 914, 914,
        915, 915, 915, 915, 915, 915, 915, 915, 916, 916, 916, 916, 916, 916, 916, 916, 917, 917, 917, 917, 917, 917, 917, 917, 918, 919, 1, 1, 1, 1, 1, 1,
        920, 920, 920, 920, 920, 920, 920, 920, 921, 921, 921, 921, 921, 921, 921, 921, 922, 922, 922, 922, 922, 922, 922, 922, 923, 923, 923, 923, 923, 923, 923, 923,
        924, 924, 924, 924, 924, 924, 924, 924, 925, 925, 925, 925, 925, 925, 925, 925, 926, 926, 926, 926, 926, 926, 926, 926, 927, 928, 1, 1, 1, 1, 1, 1,
        973, 973, 973, 973, 973, 973, 973, 973, 974, 974, 974, 974, 974, 974, 974, 974, 975, 975, 975, 975, 975, 975, 975, 975, 976, 976, 976, 976, 976, 976, 976, 977,
        978, 978, 978, 978, 978, 978, 978, 979, 980, 980, 980, 980, 980, 980, 980, 980, 981, 981, 981, 981, 981, 981, 981, 981, 982, 983, 1, 1, 1, 1, 1, 1,
        862, 862, 862, 862, 862, 862, 862, 862, 1, 1, 1, 1, 1, 1, 1, 1, 863, 863, 863, 863, 863, 863, 863, 863, 864, 864, 864, 864, 864, 864, 864, 865,
        866, 866, 866, 866, 866, 866, 866, 867, 868, 868, 868, 868, 868, 868, 868, 868, 869, 869, 869, 869, 869, 869, 869, 869, 870, 871, 1, 1, 1, 1, 1, 1,
        872, 872, 872, 872, 872, 872, 872, 872, 873, 873, 873, 873, 873, 873, 873, 873, 874, 874, 874, 874, 874, 874, 874, 874, 875, 875, 875, 875, 875, 875, 875, 875,
        876, 876, 876, 876, 876, 876, 876, 876, 877, 877, 877, 877, 877, 877, 877, 877, 878, 878, 878, 878, 878, 878, 878, 878, 879, 880, 1, 1, 1, 1, 1, 1,
        881, 881, 881, 881, 881, 881, 881, 881, 882, 882, 882, 882, 882, 882, 882, 882, 883, 883, 883, 883, 883, 883, 883, 883, 884, 884, 884, 884, 884, 884, 884, 884,
        885, 885, 885, 885, 885, 885, 885, 885, 886, 886, 886, 886, 886, 886, 886, 886, 887, 887, 887, 887, 887, 887, 887, 887, 888, 889, 1, 1, 1, 1, 1, 1,
        984, 984, 984, 984, 984, 984, 984, 984, 985, 985, 985, 985, 985, 985, 985, 985, 986, 986, 986, 986, 986, 986, 986, 986, 987, 987, 987, 987, 987, 987, 987, 988,
        989, 989, 989, 989, 989, 989, 989, 990, 991, 991, 991, 991, 991, 991, 991, 991, 992, 992, 992, 992, 992, 992, 992, 992, 993, 994, 1, 1, 1, 1, 1, 1,
        901, 901, 901, 901, 901, 901, 901, 901, 1, 1, 1, 1, 1, 1, 1, 1, 902, 902, 902, 902, 902, 902, 902, 902, 903, 903, 903, 903, 903, 903, 903, 904,
        905, 905, 905, 905, 905, 905, 905, 906, 907, 907, 907, 907, 907, 907, 907, 907, 908, 908, 908, 908, 908, 908, 908, 908, 909, 910, 1, 1, 1, 1, 1, 1,
        911, 911, 911, 911, 911, 911, 911, 911, 912, 912, 912, 912, 912, 912, 912, 912, 913, 913, 913, 913, 913, 913, 913, 913, 914, 914, 914, 914, 914, 914, 914, 914,
        915, 915, 915, 915, 915, 915, 915, 915, 916, 916, 916, 916, 916, 916, 916, 916, 917, 917, 917, 917, 917, 917, 917, 917, 918, 919, 1, 1, 1, 1, 1, 1,
        920, 920, 920, 920, 920, 920, 920, 920, 921, 921, 921, 921, 921, 921, 921, 921, 922, 922, 922, 922, 922, 922, 922, 922, 923, 923, 923, 923, 923, 923, 923, 923,
        924, 924, 924, 924, 924, 924, 924, 924, 925, 925, 925, 925, 925, 925, 925, 925, 926, 926, 926, 926, 926, 926, 926, 926, 927, 928, 1, 1, 1, 1, 1, 1,
        995, 995, 995, 995, 995, 995, 995, 995, 996, 996, 996, 996, 996, 996, 996, 996, 997, 997, 997, 997, 997, 997, 997, 997, 998, 998, 998, 998, 998, 998, 998, 999,
        1000, 1000, 1000, 1000, 1000, 1000, 1000, 1001, 1002, 1002, 1002, 1002, 1002, 1002, 1002, 1002, 1003, 1003, 1003, 1003, 1003, 1003, 1003, 1003, 1004, 1005, 1, 1, 1, 1, 1, 1,
        862, 862, 862, 862, 862, 862, 862, 862, 1, 1, 1, 1, 1, 1, 1, 1, 863, 863, 863, 863, 863, 863, 863, 863, 864, 864, 864, 864, 864, 864, 864, 865,
        866, 866, 866, 866, 866, 866, 866, 867, 868, 868, 868, 868, 868, 868, 868, 868, 869, 869, 869, 869, 869, 869, 869, 869, 870, 871, 1, 1, 1, 1, 1, 1,
        872, 872, 872, 872, 872, 872, 872, 872, 873, 873, 873, 873, 873, 873, 873, 873, 874, 874, 874, 874, 874, 874, 874, 874, 875, 875, 875, 875, 875, 875, 875, 875,
        876, 876, 876, 876, 876, 876, 876, 876, 877, 877, 877, 877, 877, 877, 877, 877, 878, 878, 878, 878, 878, 878, 878, 878, 879, 880, 1, 1, 1, 1, 1, 1,
        881, 881, 881, 881, 881, 881, 881, 881, 882, 882, 882, 882, 882, 882, 882, 882, 883, 883, 883, 883, 883, 883, 883, 883, 884, 884, 884, 884, 884, 884, 884, 884,
        885, 885, 885, 885, 885, 885, 885, 885, 886, 886, 886, 886, 886, 886, 886, 886, 887, 887, 887, 887, 887, 887, 887, 887, 888, 889, 1, 1, 1, 1, 1, 1,
        1006, 1006, 1006, 1006, 1006, 1006, 1006, 1006, 1007, 1007, 1007, 1007, 1007, 1007, 1007, 1007, 1008, 1008, 1008, 1008, 1008, 1008, 1008, 1008, 1009, 1009, 1009, 1009, 1009, 1009, 1009, 1010,
        1011, 1011, 1011, 1011, 1011, 1011, 1011, 1012, 1013, 1013, 1013, 1013, 1013, 1013, 1013, 1013, 1014, 1014, 1014, 1014, 1014, 1014, 1014, 1014, 1015, 1016, 1, 1, 1, 1, 1, 1,
        901, 901, 901, 901, 901, 901, 901, 901, 1, 1, 1, 1, 1, 1, 1, 1, 902, 902, 902, 902, 902, 902, 902, 902, 903, 903, 903, 903, 903, 903, 903, 904,
        905, 905, 905, 905, 905, 905, 905, 906, 907, 907, 907, 907, 907, 907, 907, 907, 908, 908, 908, 908, 908, 908, 908, 908, 909, 910, 1, 1, 1, 1, 1, 1,
        911, 911, 911, 911, 911, 911, 911, 911, 912, 912, 912, 912, 912, 912, 912, 912, 913, 913, 913, 913, 913, 913, 913, 913, 914, 914, 914, 914, 914, 914, 914, 914,
        915, 915, 915, 915, 915, 915, 915, 915, 916, 916, 916, 916, 916, 916, 916, 916, 917, 917, 917, 917, 917, 917, 917, 917, 918, 919, 1, 1, 1, 1, 1, 1,
        920, 920, 920, 920, 920, 920, 920, 920, 921, 921, 921, 921, 921, 921, 921, 921, 922, 922, 922, 922, 922, 922, 922, 922, 923, 923, 923, 923, 923, 923, 923, 923,
        924, 924, 924, 924, 924, 924, 924, 924, 925, 925, 925, 925, 925, 925, 925, 925, 926, 926, 926, 926, 926, 926, 926, 926, 927, 928, 1, 1, 1, 1, 1, 1,
        1017, 1017, 1017, 1017, 1017, 1017, 1017, 1017, 1018, 1018, 1018, 1018, 1018, 1018, 1018, 1018, 1019, 1019, 1019, 1019, 1019, 1019, 1019, 1019, 1020, 1020, 1020, 1020, 1020, 1020, 1020, 1021,
        1022, 1022, 1022, 1022, 1022, 1022, 1022, 1023, 1024, 1024, 1024, 1024, 1024, 1024, 1024, 1024, 1025, 1025, 1025, 1025, 1025, 1025, 1025, 1025, 1026, 1027, 1, 1, 1, 1, 1, 1,
        862, 862, 862, 862, 862, 862, 862, 862, 1, 1, 1, 1, 1, 1, 1, 1, 863, 863, 863, 863, 863, 863, 863, 863, 864, 864, 864, 864, 864, 864, 864, 865,
        866, 866, 866, 866, 866, 866, 866, 867, 868, 868, 868, 868, 868, 868, 868, 868, 869, 869, 869, 869, 869, 869, 869, 869, 870, 871, 1, 1, 1, 1, 1, 1,
        872, 872, 872, 872, 872, 872, 872, 872, 873, 873, 873, 873, 873, 873, 873, 873, 874, 874, 874, 874, 874, 874, 874, 874, 875, 875, 875, 875, 875, 875, 875, 875,
        876, 876, 876, 876, 876, 876, 876, 876, 877, 877, 877, 877, 877, 877, 877, 877, 878, 878, 878, 878, 878, 878, 878, 878, 879, 880, 1, 1, 1, 1, 1, 1,
        881, 881, 881, 881, 881, 881, 881, 881, 882, 882, 882, 882, 882, 882, 882, 882, 883, 883, 883, 883, 883, 883, 883, 883, 884, 884, 884, 884, 884, 884, 884, 884,
        885, 885, 885, 885, 885, 885, 885, 885, 886, 886, 886, 886, 886, 886, 886, 886, 887, 887, 887, 887, 887, 887, 887, 887, 888, 889, 1, 1, 1, 1, 1, 1,
        1028, 1028, 1028, 1028, 1028, 1028, 1028, 1028, 1029, 1029, 1029, 1029, 1029, 1029, 1029, 1029, 1030, 1030, 1030, 1030, 1030, 1030, 1030, 1030, 1031, 1031, 1031, 1031, 1031, 1031, 1031, 1032,
        1033, 1033, 1033, 1033, 1033, 1033, 1033, 1034, 1035, 1035, 1035, 1035, 1035, 1035, 1035, 1035, 1036, 1036, 1036, 1036, 1036, 1036, 1036, 1036, 1037, 1038, 1, 1, 1, 1, 1, 1,
        901, 901, 901, 901, 901, 901, 901, 901, 1, 1, 1, 1, 1, 1, 1, 1, 902, 902, 902, 902, 902, 902, 902, 902, 903, 903, 903, 903, 903, 903, 903, 904,
        905, 905, 905, 905, 905, 905, 905, 906, 907, 907, 907, 907, 907, 907, 907, 907, 908, 908, 908, 908, 908, 908, 908, 908, 909, 910, 1, 1, 1, 1, 1, 1,
        911, 911, 911, 911, 911, 911, 911, 911, 912, 912, 912, 912, 912, 912, 912, 912, 913, 913, 913, 913, 913, 913, 913, 913, 914, 914, 914, 914, 914, 914, 914, 914,
        915, 915, 915, 915, 915, 915, 915, 915, 916, 916, 916, 916, 916, 916, 916, 916, 917, 917, 917, 917, 917, 917, 917, 917, 918, 919, 1, 1, 1, 1, 1, 1,
        920, 920, 920, 920, 920, 920, 920, 920, 921, 921, 921, 921, 921, 921, 921, 921, 922, 922, 922, 922, 922, 922, 922, 922, 923, 923, 923, 923, 923, 923, 923, 923,
        924, 924, 924, 924, 924, 924, 924, 924, 925, 925, 925, 925, 925, 925, 925, 925, 926, 926, 926, 926, 926, 926, 926, 926, 927, 928, 1, 1, 1, 1, 1, 1,
        1039, 1039, 1039, 1039, 1039, 1039, 1039, 1039, 1040, 1040, 1040, 1040, 1040, 1040, 1040, 1040, 1041, 1041, 1041, 1041, 1041, 1041, 1041, 1041, 1042, 1042, 1042, 1042, 1042, 1042, 1042, 1043,
        1044, 1044, 1044, 1044, 1044, 1044, 1044, 1045, 1046, 1046, 1046, 1046, 1046, 1046, 1046, 1046, 1047, 1047, 1047, 1047, 1047, 1047, 1047, 1047, 1048, 1049, 1, 1, 1, 1, 1, 1,
        862, 862, 862, 862, 862, 862, 862, 862, 1, 1, 1, 1, 1, 1, 1, 1, 863, 863, 863, 863, 863, 863, 863, 863, 864, 864, 864, 864, 864, 864, 864, 865,
        866, 866, 866, 866, 866, 866, 866, 867, 868, 868, 868, 868, 868, 868, 868, 868, 869, 869, 869, 869, 869, 869, 869, 869, 870, 871, 1, 1, 1, 1, 1, 1,
        872, 872, 872, 872, 872, 872, 872, 872, 873, 873, 873, 873, 873, 873, 873, 873, 874, 874, 874, 874, 874, 874, 874, 874, 875, 875, 875, 875, 875, 875, 875, 875,
        876, 876, 876, 876, 876, 876, 876, 876, 877, 877, 877, 877, 877, 877, 877, 877, 878, 878, 878, 878, 878, 878, 878, 878, 879, 880, 1, 1, 1, 1, 1, 1,
        881, 881, 881, 881, 881, 881, 881, 881, 882, 882, 882, 882, 882, 882, 882, 882, 883, 883, 883, 883, 883, 883, 883, 883, 884, 884, 884, 884, 884, 884, 884, 884,
        885, 885, 885, 885, 885, 885, 885, 885, 886, 886, 886, 886, 886, 886, 886, 886, 887, 887, 887, 887, 887, 887, 887, 887, 888, 889, 1, 1, 1, 1, 1, 1,
        1050, 1050, 1050, 1050, 1050, 1050, 1050, 1050, 1051, 1051, 1051, 1051, 1051, 1051, 1051, 1051, 1052, 1052, 1052, 1052, 1052, 1052, 1052, 1052, 1053, 1053, 1053, 1053, 1053, 1053, 1053, 1054,
        1055, 1055, 1055, 1055, 1055, 1055, 1055, 1056, 1057, 1057, 1057, 1057, 1057, 1057, 1057, 1057, 1058, 1058, 1058, 1058, 1058, 1058, 1058, 1058, 1059, 1060, 1, 1, 1, 1, 1, 1,
        901, 901, 901, 901, 901, 901, 901, 901, 1, 1, 1, 1, 1, 1, 1, 1, 902, 902, 902, 902, 902, 902, 902, 902, 903, 903, 903, 903, 903, 903, 903, 904,
        905, 905, 905, 905, 905, 905, 905, 906, 907, 907, 907, 907, 907, 907, 907, 907, 908, 908, 908, 908, 908, 908, 908, 908, 909, 910, 1, 1, 1, 1, 1, 1,
        911, 911, 911, 911, 911, 911, 911, 911, 912, 912, 912, 912, 912, 912, 912, 912, 913, 913, 913, 913, 913, 913, 913, 913, 914, 914, 914, 914, 914, 914, 914, 914,
        915, 915, 915, 915, 915, 915, 915, 915, 916, 916, 916, 916, 916, 916, 916, 916, 917, 917, 917, 917, 917, 917, 917, 917, 918, 919, 1, 1, 1, 1, 1, 1,
        920, 920, 920, 920, 920, 920, 920, 920, 921, 921, 921, 921, 921, 921, 921, 921, 922, 922, 922, 922, 922, 922, 922, 922, 923, 923, 923, 923, 923, 923, 923, 923,
        924, 924, 924, 924, 924, 924, 924, 924, 925, 925, 925, 925, 925, 925, 925, 925, 926, 926, 926, 926, 926, 926, 926, 926, 927, 928, 1, 1, 1, 1, 1, 1,
        1061, 1061, 1061, 1061, 1061, 1061, 1061, 1061, 1062, 1062, 1062, 1062, 1062, 1062, 1062, 1062, 1063, 1063, 1063, 1063, 1063, 1063, 1063, 1063, 1064, 1064, 1064, 1064, 1064, 1064, 1064, 1065,
        1066, 1066, 1066, 1066, 1066, 1066, 1066, 1067, 1068, 1068, 1068, 1068, 1068, 1068, 1068, 1068, 1069, 1069, 1069, 1069, 1069, 1069, 1069, 1069, 1070, 1071, 1, 1, 1, 1, 1, 1,
        862, 862, 862, 862, 862, 862, 862, 862, 1, 1, 1, 1, 1, 1, 1, 1, 863, 863, 863, 863, 863, 863, 863, 863, 864, 864, 864, 864, 864, 864, 864, 865,
        866, 866, 866, 866, 866, 866, 866, 867, 868, 868, 868, 868, 868, 868, 868, 868, 869, 869, 869, 869, 869, 869, 869, 869, 870, 871, 1, 1, 1, 1, 1, 1,
        872, 872, 872, 872, 872, 872, 872, 872, 873, 873, 873, 873, 873, 873, 873, 873, 874, 874, 874, 874, 874, 874, 874, 874, 875, 875, 875, 875, 875, 875, 875, 875,
        876, 876, 876, 876, 876, 876, 876, 876, 877, 877, 877, 877, 877, 877, 877, 877, 878, 878, 878, 878, 878, 878, 878, 878, 879, 880, 1, 1, 1, 1, 1, 1,
        881, 881, 881, 881, 881, 881, 881, 881, 882, 882, 882, 882, 882, 882, 882, 882, 883, 883, 883, 883, 883, 883, 883, 883, 884, 884, 884, 884, 884, 884, 884, 884,
        885, 885, 885, 885, 885, 885, 885, 885, 886, 886, 886, 886, 886, 886, 886, 886, 887, 887, 887, 887, 887, 887, 887, 887, 888, 889, 1, 1, 1, 1, 1, 1,
        1072, 1072, 1072, 1072, 1072, 1072, 1072, 1072, 1073, 1073, 1073, 1073, 1073, 1073, 1073, 1073, 1074, 1074, 1074, 1074, 1074, 1074, 1074, 1074, 1075, 1075, 1075, 1075, 1075, 1075, 1075, 1076,
        1077, 1077, 1077, 1077, 1077, 1077, 1077, 1078, 1079, 1079, 1079, 1079, 1079, 1079, 1079, 1079, 1080, 1080, 1080, 1080, 1080, 1080, 1080, 1080, 1081, 1082, 1, 1, 1, 1, 1, 1,
        901, 901, 901, 901, 901, 901, 901, 901, 1, 1, 1, 1, 1, 1, 1, 1, 902, 902, 902, 902, 902, 902, 902, 902, 903, 903, 903, 903, 903, 903, 903, 904,
        905, 905, 905, 905, 905, 905, 905, 906, 907, 907, 907, 907, 907, 907, 907, 907, 908, 908, 908, 908, 908, 908, 908, 908, 909, 910, 1, 1, 1, 1, 1, 1,
        911, 911, 911, 911, 911, 911, 911, 911, 912, 912, 912, 912, 912, 912, 912, 912, 913, 913, 913, 913, 913, 913, 913, 913, 914, 914, 914, 914, 914, 914, 914, 914,
        915, 915, 915, 915, 915, 915, 915, 915, 916, 916, 916, 916, 916, 916, 916, 916, 917, 917, 917, 917, 917, 917, 917, 917, 918, 919, 1, 1, 1, 1, 1, 1,
        920, 920, 920, 920, 920, 920, 920, 920, 921, 921, 921, 921, 921, 921, 921, 921, 922, 922, 922, 922, 922, 922, 922, 922, 923, 923, 923, 923, 923, 923, 923, 923,
        924, 924, 924, 924, 924, 924, 924, 924, 925, 925, 925, 925, 925, 925, 925, 925, 926, 926, 926, 926, 926, 926, 926, 926, 927, 928, 1, 1, 1, 1, 1, 1,
        1083, 1083, 1083, 1083, 1083, 1083, 1083, 1083, 1084, 1084, 1084, 1084, 1084, 1084, 1084, 1084, 1085, 1085, 1085, 1085, 1085, 1085, 1085, 1085, 1086, 1086, 1086, 1086, 1086, 1086, 1086, 1087,
        1088, 1088, 1088, 1088, 1088, 1088, 1088, 1089, 1090, 1090, 1090, 1090, 1090, 1090, 1090, 1090, 1091, 1091, 1091, 1091, 1091, 1091, 1091, 1091, 1092, 1093, 1, 1, 1, 1, 1, 1,
        1094, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095,
        1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095,
        1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095,
        1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095,
        1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095,
        1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095,
        1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095,
        1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1095, 1096,
        1097, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098,
        1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098,
        1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098,
        1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098,
        1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098,
        1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098,
        1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098,
        1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1098, 1099,
        1100, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101,
        1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101,
        1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101,
        1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101,
        1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101,
        1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101,
        1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101,
        1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1101, 1102,
        1103, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104,
        1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104,
        1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104,
        1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104,
        1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104,
        1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104,
        1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104,
        1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1104, 1105,
        1106, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107,
        1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107,
        1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107,
        1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107,
        1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107,
        1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107,
        1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107,
        1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1107, 1108,
        1109, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110,
        1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110,
        1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110,
        1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110,
        1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110,
        1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110,
        1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110,
        1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1110, 1111,
        1112, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113,
        1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113,
        1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113,
        1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113,
        1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113,
        1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113,
        1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113,
        1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1113, 1114,
        1115, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116,
        1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116,
        1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116,
        1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116,
        1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116,
        1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116,
        1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116,
        1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1116, 1117,
        1118, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119,
        1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119,
        1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119,
        1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119,
        1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119,
        1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119,
        1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119,
        1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1119, 1120,
        1121, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122,
        1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122,
        1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122,
        1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122,
        1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122,
        1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122,
        1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122,
        1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1122, 1123,
        1124, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125,
        1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125,
        1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125,
        1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125,
        1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125,
        1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125,
        1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125,
        1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1125, 1126,
        1127, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128,
        1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128,
        1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128,
        1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128,
        1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128,
        1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128,
        1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128,
        1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1128, 1129,
        1130, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131,
        1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131,
        1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131,
        1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131,
        1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131,
        1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131,
        1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131,
        1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1131, 1132,
        1133, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134,
        1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134,
        1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134,
        1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134,
        1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134,
        1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134,
        1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134,
        1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1134, 1135,
        1136, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137,
        1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137,
        1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137,
        1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137,
        1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137,
        1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137,
        1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137,
        1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1137, 1138,
        1139, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140,
        1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140,
        1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140,
        1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140,
        1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140,
        1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140,
        1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140,
        1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1140, 1141,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142, 1142,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1143, 1143, 1143, 1143, 1143, 1143, 1143, 1143, 1, 1, 1, 1, 1, 1, 1, 1, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1145, 1145, 1145, 1145, 1145, 1145, 1145, 1146,
        1147, 1147, 1147, 1147, 1147, 1147, 1147, 1148, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1151, 1152, 1153, 1154, 1155, 1, 1, 1,
        1156, 1156, 1156, 1156, 1156, 1156, 1156, 1156, 1, 1, 1, 1, 1, 1, 1, 1, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1158, 1158, 1158, 1158, 1158, 1158, 1158, 1158,
        1159, 1159, 1159, 1159, 1159, 1159, 1159, 1159, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1162, 1163, 1164, 1165, 1166, 1, 1, 1,
        1167, 1167, 1167, 1167, 1167, 1167, 1167, 1167, 1, 1, 1, 1, 1, 1, 1, 1, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1169, 1169, 1169, 1169, 1169, 1169, 1169, 1169,
        1170, 1170, 1170, 1170, 1170, 1170, 1170, 1170, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1173, 1174, 1175, 1176, 1177, 1, 1, 1,
        1178, 1178, 1178, 1178, 1178, 1178, 1178, 1178, 1, 1, 1, 1, 1, 1, 1, 1, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1180, 1180, 1180, 1180, 1180, 1180, 1180, 1180,
        1181, 1181, 1181, 1181, 1181, 1181, 1181, 1181, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1184, 1185, 1186, 1187, 1188, 1, 1, 1,
        1189, 1189, 1189, 1189, 1189, 1189, 1189, 1189, 1190, 1190, 1190, 1190, 1190, 1190, 1190, 1191, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1193, 1193, 1193, 1193, 1193, 1193, 1193, 1194,
        1195, 1195, 1195, 1195, 1195, 1195, 1195, 1196, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1199, 1200, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1202, 1202, 1202, 1202, 1202, 1202, 1202, 1202,
        1203, 1203, 1203, 1203, 1203, 1203, 1203, 1203, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1206, 1207, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1209, 1209, 1209, 1209, 1209, 1209, 1209, 1209,
        1210, 1210, 1210, 1210, 1210, 1210, 1210, 1210, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1213, 1214, 1, 1, 1, 1, 1, 1,
        1215, 1215, 1215, 1215, 1215, 1215, 1215, 1215, 1, 1, 1, 1, 1, 1, 1, 1, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1217, 1217, 1217, 1217, 1217, 1217, 1217, 1217,
        1218, 1218, 1218, 1218, 1218, 1218, 1218, 1218, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1221, 1222, 1223, 1224, 1225, 1, 1, 1,
        1143, 1143, 1143, 1143, 1143, 1143, 1143, 1143, 1, 1, 1, 1, 1, 1, 1, 1, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1145, 1145, 1145, 1145, 1145, 1145, 1145, 1146,
        1147, 1147, 1147, 1147, 1147, 1147, 1147, 1148, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1151, 1152, 1153, 1154, 1155, 1, 1, 1,
        1156, 1156, 1156, 1156, 1156, 1156, 1156, 1156, 1, 1, 1, 1, 1, 1, 1, 1, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1158, 1158, 1158, 1158, 1158, 1158, 1158, 1158,
        1159, 1159, 1159, 1159, 1159, 1159, 1159, 1159, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1162, 1163, 1164, 1165, 1166, 1, 1, 1,
        1167, 1167, 1167, 1167, 1167, 1167, 1167, 1167, 1, 1, 1, 1, 1, 1, 1, 1, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1169, 1169, 1169, 1169, 1169, 1169, 1169, 1169,
        1170, 1170, 1170, 1170, 1170, 1170, 1170, 1170, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1173, 1174, 1175, 1176, 1177, 1, 1, 1,
        1178, 1178, 1178, 1178, 1178, 1178, 1178, 1178, 1, 1, 1, 1, 1, 1, 1, 1, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1180, 1180, 1180, 1180, 1180, 1180, 1180, 1180,
        1181, 1181, 1181, 1181, 1181, 1181, 1181, 1181, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1184, 1185, 1186, 1187, 1188, 1, 1, 1,
        1189, 1189, 1189, 1189, 1189, 1189, 1189, 1189, 1190, 1190, 1190, 1190, 1190, 1190, 1190, 1191, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1193, 1193, 1193, 1193, 1193, 1193, 1193, 1194,
        1195, 1195, 1195, 1195, 1195, 1195, 1195, 1196, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1199, 1200, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1202, 1202, 1202, 1202, 1202, 1202, 1202, 1202,
        1203, 1203, 1203, 1203, 1203, 1203, 1203, 1203, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1206, 1207, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1209, 1209, 1209, 1209, 1209, 1209, 1209, 1209,
        1210, 1210, 1210, 1210, 1210, 1210, 1210, 1210, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1213, 1214, 1, 1, 1, 1, 1, 1,
        1215, 1215, 1215, 1215, 1215, 1215, 1215, 1215, 1, 1, 1, 1, 1, 1, 1, 1, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1217, 1217, 1217, 1217, 1217, 1217, 1217, 1217,
        1218, 1218, 1218, 1218, 1218, 1218, 1218, 1218, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1221, 1222, 1223, 1224, 1225, 1, 1, 1,
        1143, 1143, 1143, 1143, 1143, 1143, 1143, 1143, 1, 1, 1, 1, 1, 1, 1, 1, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1145, 1145, 1145, 1145, 1145, 1145, 1145, 1146,
        1147, 1147, 1147, 1147, 1147, 1147, 1147, 1148, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1151, 1152, 1153, 1154, 1155, 1, 1, 1,
        1156, 1156, 1156, 1156, 1156, 1156, 1156, 1156, 1, 1, 1, 1, 1, 1, 1, 1, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1158, 1158, 1158, 1158, 1158, 1158, 1158, 1158,
        1159, 1159, 1159, 1159, 1159, 1159, 1159, 1159, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1162, 1163, 1164, 1165, 1166, 1, 1, 1,
        1167, 1167, 1167, 1167, 1167, 1167, 1167, 1167, 1, 1, 1, 1, 1, 1, 1, 1, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1169, 1169, 1169, 1169, 1169, 1169, 1169, 1169,
        1170, 1170, 1170, 1170, 1170, 1170, 1170, 1170, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1173, 1174, 1175, 1176, 1177, 1, 1, 1,
        1178, 1178, 1178, 1178, 1178, 1178, 1178, 1178, 1, 1, 1, 1, 1, 1, 1, 1, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1180, 1180, 1180, 1180, 1180, 1180, 1180, 1180,
        1181, 1181, 1181, 1181, 1181, 1181, 1181, 1181, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1184, 1185, 1186, 1187, 1188, 1, 1, 1,
        1189, 1189, 1189, 1189, 1189, 1189, 1189, 1189, 1190, 1190, 1190, 1190, 1190, 1190, 1190, 1191, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1193, 1193, 1193, 1193, 1193, 1193, 1193, 1194,
        1195, 1195, 1195, 1195, 1195, 1195, 1195, 1196, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1199, 1200, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1202, 1202, 1202, 1202, 1202, 1202, 1202, 1202,
        1203, 1203, 1203, 1203, 1203, 1203, 1203, 1203, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1206, 1207, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1209, 1209, 1209, 1209, 1209, 1209, 1209, 1209,
        1210, 1210, 1210, 1210, 1210, 1210, 1210, 1210, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1213, 1214, 1, 1, 1, 1, 1, 1,
        1215, 1215, 1215, 1215, 1215, 1215, 1215, 1215, 1, 1, 1, 1, 1, 1, 1, 1, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1217, 1217, 1217, 1217, 1217, 1217, 1217, 1217,
        1218, 1218, 1218, 1218, 1218, 1218, 1218, 1218, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1221, 1222, 1223, 1224, 1225, 1, 1, 1,
        1143, 1143, 1143, 1143, 1143, 1143, 1143, 1143, 1, 1, 1, 1, 1, 1, 1, 1, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1145, 1145, 1145, 1145, 1145, 1145, 1145, 1146,
        1147, 1147, 1147, 1147, 1147, 1147, 1147, 1148, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1151, 1152, 1153, 1154, 1155, 1, 1, 1,
        1156, 1156, 1156, 1156, 1156, 1156, 1156, 1156, 1, 1, 1, 1, 1, 1, 1, 1, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1158, 1158, 1158, 1158, 1158, 1158, 1158, 1158,
        1159, 1159, 1159, 1159, 1159, 1159, 1159, 1159, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1162, 1163, 1164, 1165, 1166, 1, 1, 1,
        1167, 1167, 1167, 1167, 1167, 1167, 1167, 1167, 1, 1, 1, 1, 1, 1, 1, 1, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1169, 1169, 1169, 1169, 1169, 1169, 1169, 1169,
        1170, 1170, 1170, 1170, 1170, 1170, 1170, 1170, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1173, 1174, 1175, 1176, 1177, 1, 1, 1,
        1178, 1178, 1178, 1178, 1178, 1178, 1178, 1178, 1, 1, 1, 1, 1, 1, 1, 1, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1180, 1180, 1180, 1180, 1180, 1180, 1180, 1180,
        1181, 1181, 1181, 1181, 1181, 1181, 1181, 1181, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1184, 1185, 1186, 1187, 1188, 1, 1, 1,
        1189, 1189, 1189, 1189, 1189, 1189, 1189, 1189, 1190, 1190, 1190, 1190, 1190, 1190, 1190, 1191, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1193, 1193, 1193, 1193, 1193, 1193, 1193, 1194,
        1195, 1195, 1195, 1195, 1195, 1195, 1195, 1196, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1199, 1200, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1202, 1202, 1202, 1202, 1202, 1202, 1202, 1202,
        1203, 1203, 1203, 1203, 1203, 1203, 1203, 1203, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1206, 1207, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1209, 1209, 1209, 1209, 1209, 1209, 1209, 1209,
        1210, 1210, 1210, 1210, 1210, 1210, 1210, 1210, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1213, 1214, 1, 1, 1, 1, 1, 1,
        1215, 1215, 1215, 1215, 1215, 1215, 1215, 1215, 1, 1, 1, 1, 1, 1, 1, 1, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1217, 1217, 1217, 1217, 1217, 1217, 1217, 1217,
        1218, 1218, 1218, 1218, 1218, 1218, 1218, 1218, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1221, 1222, 1223, 1224, 1225, 1, 1, 1,
        1143, 1143, 1143, 1143, 1143, 1143, 1143, 1143, 1, 1, 1, 1, 1, 1, 1, 1, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1145, 1145, 1145, 1145, 1145, 1145, 1145, 1146,
        1147, 1147, 1147, 1147, 1147, 1147, 1147, 1148, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1151, 1152, 1153, 1154, 1155, 1, 1, 1,
        1156, 1156, 1156, 1156, 1156, 1156, 1156, 1156, 1, 1, 1, 1, 1, 1, 1, 1, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1158, 1158, 1158, 1158, 1158, 1158, 1158, 1158,
        1159, 1159, 1159, 1159, 1159, 1159, 1159, 1159, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1162, 1163, 1164, 1165, 1166, 1, 1, 1,
        1167, 1167, 1167, 1167, 1167, 1167, 1167, 1167, 1, 1, 1, 1, 1, 1, 1, 1, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1169, 1169, 1169, 1169, 1169, 1169, 1169, 1169,
        1170, 1170, 1170, 1170, 1170, 1170, 1170, 1170, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1173, 1174, 1175, 1176, 1177, 1, 1, 1,
        1178, 1178, 1178, 1178, 1178, 1178, 1178, 1178, 1, 1, 1, 1, 1, 1, 1, 1, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1180, 1180, 1180, 1180, 1180, 1180, 1180, 1180,
        1181, 1181, 1181, 1181, 1181, 1181, 1181, 1181, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1184, 1185, 1186, 1187, 1188, 1, 1, 1,
        1189, 1189, 1189, 1189, 1189, 1189, 1189, 1189, 1190, 1190, 1190, 1190, 1190, 1190, 1190, 1191, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1193, 1193, 1193, 1193, 1193, 1193, 1193, 1194,
        1195, 1195, 1195, 1195, 1195, 1195, 1195, 1196, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1199, 1200, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1202, 1202, 1202, 1202, 1202, 1202, 1202, 1202,
        1203, 1203, 1203, 1203, 1203, 1203, 1203, 1203, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1206, 1207, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1209, 1209, 1209, 1209, 1209, 1209, 1209, 1209,
        1210, 1210, 1210, 1210, 1210, 1210, 1210, 1210, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1213, 1214, 1, 1, 1, 1, 1, 1,
        1215, 1215, 1215, 1215, 1215, 1215, 1215, 1215, 1, 1, 1, 1, 1, 1, 1, 1, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1217, 1217, 1217, 1217, 1217, 1217, 1217, 1217,
        1218, 1218, 1218, 1218, 1218, 1218, 1218, 1218, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1221, 1222, 1223, 1224, 1225, 1, 1, 1,
        1143, 1143, 1143, 1143, 1143, 1143, 1143, 1143, 1, 1, 1, 1, 1, 1, 1, 1, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1145, 1145, 1145, 1145, 1145, 1145, 1145, 1146,
        1147, 1147, 1147, 1147, 1147, 1147, 1147, 1148, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1151, 1152, 1153, 1154, 1155, 1, 1, 1,
        1156, 1156, 1156, 1156, 1156, 1156, 1156, 1156, 1, 1, 1, 1, 1, 1, 1, 1, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1158, 1158, 1158, 1158, 1158, 1158, 1158, 1158,
        1159, 1159, 1159, 1159, 1159, 1159, 1159, 1159, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1162, 1163, 1164, 1165, 1166, 1, 1, 1,
        1167, 1167, 1167, 1167, 1167, 1167, 1167, 1167, 1, 1, 1, 1, 1, 1, 1, 1, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1169, 1169, 1169, 1169, 1169, 1169, 1169, 1169,
        1170, 1170, 1170, 1170, 1170, 1170, 1170, 1170, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1173, 1174, 1175, 1176, 1177, 1, 1, 1,
        1178, 1178, 1178, 1178, 1178, 1178, 1178, 1178, 1, 1, 1, 1, 1, 1, 1, 1, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1180, 1180, 1180, 1180, 1180, 1180, 1180, 1180,
        1181, 1181, 1181, 1181, 1181, 1181, 1181, 1181, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1184, 1185, 1186, 1187, 1188, 1, 1, 1,
        1189, 1189, 1189, 1189, 1189, 1189, 1189, 1189, 1190, 1190, 1190, 1190, 1190, 1190, 1190, 1191, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1193, 1193, 1193, 1193, 1193, 1193, 1193, 1194,
        1195, 1195, 1195, 1195, 1195, 1195, 1195, 1196, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1199, 1200, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1202, 1202, 1202, 1202, 1202, 1202, 1202, 1202,
        1203, 1203, 1203, 1203, 1203, 1203, 1203, 1203, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1206, 1207, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1209, 1209, 1209, 1209, 1209, 1209, 1209, 1209,
        1210, 1210, 1210, 1210, 1210, 1210, 1210, 1210, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1213, 1214, 1, 1, 1, 1, 1, 1,
        1215, 1215, 1215, 1215, 1215, 1215, 1215, 1215, 1, 1, 1, 1, 1, 1, 1, 1, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1217, 1217, 1217, 1217, 1217, 1217, 1217, 1217,
        1218, 1218, 1218, 1218, 1218, 1218, 1218, 1218, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1221, 1222, 1223, 1224, 1225, 1, 1, 1,
        1143, 1143, 1143, 1143, 1143, 1143, 1143, 1143, 1, 1, 1, 1, 1, 1, 1, 1, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1145, 1145, 1145, 1145, 1145, 1145, 1145, 1146,
        1147, 1147, 1147, 1147, 1147, 1147, 1147, 1148, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1151, 1152, 1153, 1154, 1155, 1, 1, 1,
        1156, 1156, 1156, 1156, 1156, 1156, 1156, 1156, 1, 1, 1, 1, 1, 1, 1, 1, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1158, 1158, 1158, 1158, 1158, 1158, 1158, 1158,
        1159, 1159, 1159, 1159, 1159, 1159, 1159, 1159, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1162, 1163, 1164, 1165, 1166, 1, 1, 1,
        1167, 1167, 1167, 1167, 1167, 1167, 1167, 1167, 1, 1, 1, 1, 1, 1, 1, 1, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1169, 1169, 1169, 1169, 1169, 1169, 1169, 1169,
        1170, 1170, 1170, 1170, 1170, 1170, 1170, 1170, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1173, 1174, 1175, 1176, 1177, 1, 1, 1,
        1178, 1178, 1178, 1178, 1178, 1178, 1178, 1178, 1, 1, 1, 1, 1, 1, 1, 1, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1180, 1180, 1180, 1180, 1180, 1180, 1180, 1180,
        1181, 1181, 1181, 1181, 1181, 1181, 1181, 1181, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1184, 1185, 1186, 1187, 1188, 1, 1, 1,
        1189, 1189, 1189, 1189, 1189, 1189, 1189, 1189, 1190, 1190, 1190, 1190, 1190, 1190, 1190, 1191, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1193, 1193, 1193, 1193, 1193, 1193, 1193, 1194,
        1195, 1195, 1195, 1195, 1195, 1195, 1195, 1196, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1199, 1200, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1202, 1202, 1202, 1202, 1202, 1202, 1202, 1202,
        1203, 1203, 1203, 1203, 1203, 1203, 1203, 1203, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1206, 1207, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1209, 1209, 1209, 1209, 1209, 1209, 1209, 1209,
        1210, 1210, 1210, 1210, 1210, 1210, 1210, 1210, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1213, 1214, 1, 1, 1, 1, 1, 1,
        1215, 1215, 1215, 1215, 1215, 1215, 1215, 1215, 1, 1, 1, 1, 1, 1, 1, 1, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1217, 1217, 1217, 1217, 1217, 1217, 1217, 1217,
        1218, 1218, 1218, 1218, 1218, 1218, 1218, 1218, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1221, 1222, 1223, 1224, 1225, 1, 1, 1,
        1143, 1143, 1143, 1143, 1143, 1143, 1143, 1143, 1, 1, 1, 1, 1, 1, 1, 1, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1144, 1145, 1145, 1145, 1145, 1145, 1145, 1145, 1146,
        1147, 1147, 1147, 1147, 1147, 1147, 1147, 1148, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1149, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1150, 1151, 1152, 1153, 1154, 1155, 1, 1, 1,
        1156, 1156, 1156, 1156, 1156, 1156, 1156, 1156, 1, 1, 1, 1, 1, 1, 1, 1, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1157, 1158, 1158, 1158, 1158, 1158, 1158, 1158, 1158,
        1159, 1159, 1159, 1159, 1159, 1159, 1159, 1159, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1160, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1161, 1162, 1163, 1164, 1165, 1166, 1, 1, 1,
        1167, 1167, 1167, 1167, 1167, 1167, 1167, 1167, 1, 1, 1, 1, 1, 1, 1, 1, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1168, 1169, 1169, 1169, 1169, 1169, 1169, 1169, 1169,
        1170, 1170, 1170, 1170, 1170, 1170, 1170, 1170, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1171, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1172, 1173, 1174, 1175, 1176, 1177, 1, 1, 1,
        1178, 1178, 1178, 1178, 1178, 1178, 1178, 1178, 1, 1, 1, 1, 1, 1, 1, 1, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1179, 1180, 1180, 1180, 1180, 1180, 1180, 1180, 1180,
        1181, 1181, 1181, 1181, 1181, 1181, 1181, 1181, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1182, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1183, 1184, 1185, 1186, 1187, 1188, 1, 1, 1,
        1189, 1189, 1189, 1189, 1189, 1189, 1189, 1189, 1226, 1226, 1226, 1226, 1226, 1226, 1226, 1227, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1192, 1193, 1193, 1193, 1193, 1193, 1193, 1193, 1194,
        1195, 1195, 1195, 1195, 1195, 1195, 1195, 1196, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1197, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1198, 1199, 1200, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1201, 1202, 1202, 1202, 1202, 1202, 1202, 1202, 1202,
        1203, 1203, 1203, 1203, 1203, 1203, 1203, 1203, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1204, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1205, 1206, 1207, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1208, 1209, 1209, 1209, 1209, 1209, 1209, 1209, 1209,
        1210, 1210, 1210, 1210, 1210, 1210, 1210, 1210, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1211, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1212, 1213, 1214, 1, 1, 1, 1, 1, 1,
        1215, 1215, 1215, 1215, 1215, 1215, 1215, 1215, 1, 1, 1, 1, 1, 1, 1, 1, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1216, 1217, 1217, 1217, 1217, 1217, 1217, 1217, 1217,
        1218, 1218, 1218, 1218, 1218, 1218, 1218, 1218, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1219, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1220, 1221, 1222, 1223, 1224, 1225, 1, 1, 1,
        1228, 1228, 1228, 1228, 1228, 1228, 1228, 1228, 1, 1, 1, 1, 1, 1, 1, 1, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1230, 1230, 1230, 1230, 1230, 1230, 1230, 1231,
        1232, 1232, 1232, 1232, 1232, 1232, 1232, 1233, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1236, 1237, 1238, 1239, 1240, 1, 1, 1,
        1241, 1241, 1241, 1241, 1241, 1241, 1241, 1241, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1244, 1244, 1244, 1244, 1244, 1244, 1244, 1244,
        1245, 1245, 1245, 1245, 1245, 1245, 1245, 1245, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1248, 1249, 1250, 1251, 1252, 1, 1, 1,
        1253, 1253, 1253, 1253, 1253, 1253, 1253, 1253, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1256, 1256, 1256, 1256, 1256, 1256, 1256, 1256,
        1257, 1257, 1257, 1257, 1257, 1257, 1257, 1257, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1260, 1261, 1262, 1263, 1264, 1, 1, 1,
        1265, 1265, 1265, 1265, 1265, 1265, 1265, 1265, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1268, 1268, 1268, 1268, 1268, 1268, 1268, 1268,
        1269, 1269, 1269, 1269, 1269, 1269, 1269, 1269, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1272, 1273, 1274, 1275, 1276, 1, 1, 1,
        1277, 1277, 1277, 1277, 1277, 1277, 1277, 1277, 1278, 1278, 1278, 1278, 1278, 1278, 1278, 1279, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1281, 1281, 1281, 1281, 1281, 1281, 1281, 1282,
        1283, 1283, 1283, 1283, 1283, 1283, 1283, 1284, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1287, 1288, 1, 1, 1, 1, 1, 1,
        1289, 1289, 1289, 1289, 1289, 1289, 1289, 1289, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1292, 1292, 1292, 1292, 1292, 1292, 1292, 1292,
        1293, 1293, 1293, 1293, 1293, 1293, 1293, 1293, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1296, 1297, 1, 1, 1, 1, 1, 1,
        1298, 1298, 1298, 1298, 1298, 1298, 1298, 1298, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1301, 1301, 1301, 1301, 1301, 1301, 1301, 1301,
        1302, 1302, 1302, 1302, 1302, 1302, 1302, 1302, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1305, 1306, 1, 1, 1, 1, 1, 1,
        1307, 1307, 1307, 1307, 1307, 1307, 1307, 1307, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1310, 1310, 1310, 1310, 1310, 1310, 1310, 1310,
        1311, 1311, 1311, 1311, 1311, 1311, 1311, 1311, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1314, 1315, 1316, 1317, 1318, 1, 1, 1,
        1228, 1228, 1228, 1228, 1228, 1228, 1228, 1228, 1, 1, 1, 1, 1, 1, 1, 1, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1230, 1230, 1230, 1230, 1230, 1230, 1230, 1231,
        1232, 1232, 1232, 1232, 1232, 1232, 1232, 1233, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1236, 1237, 1238, 1239, 1240, 1, 1, 1,
        1241, 1241, 1241, 1241, 1241, 1241, 1241, 1241, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1244, 1244, 1244, 1244, 1244, 1244, 1244, 1244,
        1245, 1245, 1245, 1245, 1245, 1245, 1245, 1245, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1248, 1249, 1250, 1251, 1252, 1, 1, 1,
        1253, 1253, 1253, 1253, 1253, 1253, 1253, 1253, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1256, 1256, 1256, 1256, 1256, 1256, 1256, 1256,
        1257, 1257, 1257, 1257, 1257, 1257, 1257, 1257, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1260, 1261, 1262, 1263, 1264, 1, 1, 1,
        1265, 1265, 1265, 1265, 1265, 1265, 1265, 1265, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1268, 1268, 1268, 1268, 1268, 1268, 1268, 1268,
        1269, 1269, 1269, 1269, 1269, 1269, 1269, 1269, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1272, 1273, 1274, 1275, 1276, 1, 1, 1,
        1277, 1277, 1277, 1277, 1277, 1277, 1277, 1277, 1278, 1278, 1278, 1278, 1278, 1278, 1278, 1279, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1281, 1281, 1281, 1281, 1281, 1281, 1281, 1282,
        1283, 1283, 1283, 1283, 1283, 1283, 1283, 1284, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1287, 1288, 1, 1, 1, 1, 1, 1,
        1289, 1289, 1289, 1289, 1289, 1289, 1289, 1289, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1292, 1292, 1292, 1292, 1292, 1292, 1292, 1292,
        1293, 1293, 1293, 1293, 1293, 1293, 1293, 1293, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1296, 1297, 1, 1, 1, 1, 1, 1,
        1298, 1298, 1298, 1298, 1298, 1298, 1298, 1298, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1301, 1301, 1301, 1301, 1301, 1301, 1301, 1301,
        1302, 1302, 1302, 1302, 1302, 1302, 1302, 1302, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1305, 1306, 1, 1, 1, 1, 1, 1,
        1307, 1307, 1307, 1307, 1307, 1307, 1307, 1307, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1310, 1310, 1310, 1310, 1310, 1310, 1310, 1310,
        1311, 1311, 1311, 1311, 1311, 1311, 1311, 1311, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1314, 1315, 1316, 1317, 1318, 1, 1, 1,
        1228, 1228, 1228, 1228, 1228, 1228, 1228, 1228, 1, 1, 1, 1, 1, 1, 1, 1, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1230, 1230, 1230, 1230, 1230, 1230, 1230, 1231,
        1232, 1232, 1232, 1232, 1232, 1232, 1232, 1233, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1236, 1237, 1238, 1239, 1240, 1, 1, 1,
        1241, 1241, 1241, 1241, 1241, 1241, 1241, 1241, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1244, 1244, 1244, 1244, 1244, 1244, 1244, 1244,
        1245, 1245, 1245, 1245, 1245, 1245, 1245, 1245, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1248, 1249, 1250, 1251, 1252, 1, 1, 1,
        1253, 1253, 1253, 1253, 1253, 1253, 1253, 1253, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1256, 1256, 1256, 1256, 1256, 1256, 1256, 1256,
        1257, 1257, 1257, 1257, 1257, 1257, 1257, 1257, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1260, 1261, 1262, 1263, 1264, 1, 1, 1,
        1265, 1265, 1265, 1265, 1265, 1265, 1265, 1265, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1268, 1268, 1268, 1268, 1268, 1268, 1268, 1268,
        1269, 1269, 1269, 1269, 1269, 1269, 1269, 1269, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1272, 1273, 1274, 1275, 1276, 1, 1, 1,
        1277, 1277, 1277, 1277, 1277, 1277, 1277, 1277, 1278, 1278, 1278, 1278, 1278, 1278, 1278, 1279, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1281, 1281, 1281, 1281, 1281, 1281, 1281, 1282,
        1283, 1283, 1283, 1283, 1283, 1283, 1283, 1284, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1287, 1288, 1, 1, 1, 1, 1, 1,
        1289, 1289, 1289, 1289, 1289, 1289, 1289, 1289, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1292, 1292, 1292, 1292, 1292, 1292, 1292, 1292,
        1293, 1293, 1293, 1293, 1293, 1293, 1293, 1293, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1296, 1297, 1, 1, 1, 1, 1, 1,
        1298, 1298, 1298, 1298, 1298, 1298, 1298, 1298, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1301, 1301, 1301, 1301, 1301, 1301, 1301, 1301,
        1302, 1302, 1302, 1302, 1302, 1302, 1302, 1302, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1305, 1306, 1, 1, 1, 1, 1, 1,
        1307, 1307, 1307, 1307, 1307, 1307, 1307, 1307, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1310, 1310, 1310, 1310, 1310, 1310, 1310, 1310,
        1311, 1311, 1311, 1311, 1311, 1311, 1311, 1311, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1314, 1315, 1316, 1317, 1318, 1, 1, 1,
        1228, 1228, 1228, 1228, 1228, 1228, 1228, 1228, 1, 1, 1, 1, 1, 1, 1, 1, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1230, 1230, 1230, 1230, 1230, 1230, 1230, 1231,
        1232, 1232, 1232, 1232, 1232, 1232, 1232, 1233, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1236, 1237, 1238, 1239, 1240, 1, 1, 1,
        1241, 1241, 1241, 1241, 1241, 1241, 1241, 1241, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1244, 1244, 1244, 1244, 1244, 1244, 1244, 1244,
        1245, 1245, 1245, 1245, 1245, 1245, 1245, 1245, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1248, 1249, 1250, 1251, 1252, 1, 1, 1,
        1253, 1253, 1253, 1253, 1253, 1253, 1253, 1253, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1256, 1256, 1256, 1256, 1256, 1256, 1256, 1256,
        1257, 1257, 1257, 1257, 1257, 1257, 1257, 1257, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1260, 1261, 1262, 1263, 1264, 1, 1, 1,
        1265, 1265, 1265, 1265, 1265, 1265, 1265, 1265, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1268, 1268, 1268, 1268, 1268, 1268, 1268, 1268,
        1269, 1269, 1269, 1269, 1269, 1269, 1269, 1269, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1272, 1273, 1274, 1275, 1276, 1, 1, 1,
        1277, 1277, 1277, 1277, 1277, 1277, 1277, 1277, 1278, 1278, 1278, 1278, 1278, 1278, 1278, 1279, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1281, 1281, 1281, 1281, 1281, 1281, 1281, 1282,
        1283, 1283, 1283, 1283, 1283, 1283, 1283, 1284, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1287, 1288, 1, 1, 1, 1, 1, 1,
        1289, 1289, 1289, 1289, 1289, 1289, 1289, 1289, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1292, 1292, 1292, 1292, 1292, 1292, 1292, 1292,
        1293, 1293, 1293, 1293, 1293, 1293, 1293, 1293, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1296, 1297, 1, 1, 1, 1, 1, 1,
        1298, 1298, 1298, 1298, 1298, 1298, 1298, 1298, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1301, 1301, 1301, 1301, 1301, 1301, 1301, 1301,
        1302, 1302, 1302, 1302, 1302, 1302, 1302, 1302, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1305, 1306, 1, 1, 1, 1, 1, 1,
        1307, 1307, 1307, 1307, 1307, 1307, 1307, 1307, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1310, 1310, 1310, 1310, 1310, 1310, 1310, 1310,
        1311, 1311, 1311, 1311, 1311, 1311, 1311, 1311, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1314, 1315, 1316, 1317, 1318, 1, 1, 1,
        1228, 1228, 1228, 1228, 1228, 1228, 1228, 1228, 1, 1, 1, 1, 1, 1, 1, 1, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1230, 1230, 1230, 1230, 1230, 1230, 1230, 1231,
        1232, 1232, 1232, 1232, 1232, 1232, 1232, 1233, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1236, 1237, 1238, 1239, 1240, 1, 1, 1,
        1241, 1241, 1241, 1241, 1241, 1241, 1241, 1241, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1244, 1244, 1244, 1244, 1244, 1244, 1244, 1244,
        1245, 1245, 1245, 1245, 1245, 1245, 1245, 1245, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1248, 1249, 1250, 1251, 1252, 1, 1, 1,
        1253, 1253, 1253, 1253, 1253, 1253, 1253, 1253, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1256, 1256, 1256, 1256, 1256, 1256, 1256, 1256,
        1257, 1257, 1257, 1257, 1257, 1257, 1257, 1257, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1260, 1261, 1262, 1263, 1264, 1, 1, 1,
        1265, 1265, 1265, 1265, 1265, 1265, 1265, 1265, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1268, 1268, 1268, 1268, 1268, 1268, 1268, 1268,
        1269, 1269, 1269, 1269, 1269, 1269, 1269, 1269, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1272, 1273, 1274, 1275, 1276, 1, 1, 1,
        1277, 1277, 1277, 1277, 1277, 1277, 1277, 1277, 1278, 1278, 1278, 1278, 1278, 1278, 1278, 1279, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1281, 1281, 1281, 1281, 1281, 1281, 1281, 1282,
        1283, 1283, 1283, 1283, 1283, 1283, 1283, 1284, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1287, 1288, 1, 1, 1, 1, 1, 1,
        1289, 1289, 1289, 1289, 1289, 1289, 1289, 1289, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1292, 1292, 1292, 1292, 1292, 1292, 1292, 1292,
        1293, 1293, 1293, 1293, 1293, 1293, 1293, 1293, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1296, 1297, 1, 1, 1, 1, 1, 1,
        1298, 1298, 1298, 1298, 1298, 1298, 1298, 1298, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1301, 1301, 1301, 1301, 1301, 1301, 1301, 1301,
        1302, 1302, 1302, 1302, 1302, 1302, 1302, 1302, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1305, 1306, 1, 1, 1, 1, 1, 1,
        1307, 1307, 1307, 1307, 1307, 1307, 1307, 1307, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1310, 1310, 1310, 1310, 1310, 1310, 1310, 1310,
        1311, 1311, 1311, 1311, 1311, 1311, 1311, 1311, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1314, 1315, 1316, 1317, 1318, 1, 1, 1,
        1228, 1228, 1228, 1228, 1228, 1228, 1228, 1228, 1, 1, 1, 1, 1, 1, 1, 1, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1230, 1230, 1230, 1230, 1230, 1230, 1230, 1231,
        1232, 1232, 1232, 1232, 1232, 1232, 1232, 1233, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1236, 1237, 1238, 1239, 1240, 1, 1, 1,
        1241, 1241, 1241, 1241, 1241, 1241, 1241, 1241, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1244, 1244, 1244, 1244, 1244, 1244, 1244, 1244,
        1245, 1245, 1245, 1245, 1245, 1245, 1245, 1245, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1248, 1249, 1250, 1251, 1252, 1, 1, 1,
        1253, 1253, 1253, 1253, 1253, 1253, 1253, 1253, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1256, 1256, 1256, 1256, 1256, 1256, 1256, 1256,
        1257, 1257, 1257, 1257, 1257, 1257, 1257, 1257, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1260, 1261, 1262, 1263, 1264, 1, 1, 1,
        1265, 1265, 1265, 1265, 1265, 1265, 1265, 1265, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1268, 1268, 1268, 1268, 1268, 1268, 1268, 1268,
        1269, 1269, 1269, 1269, 1269, 1269, 1269, 1269, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1272, 1273, 1274, 1275, 1276, 1, 1, 1,
        1277, 1277, 1277, 1277, 1277, 1277, 1277, 1277, 1278, 1278, 1278, 1278, 1278, 1278, 1278, 1279, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1281, 1281, 1281, 1281, 1281, 1281, 1281, 1282,
        1283, 1283, 1283, 1283, 1283, 1283, 1283, 1284, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1287, 1288, 1, 1, 1, 1, 1, 1,
        1289, 1289, 1289, 1289, 1289, 1289, 1289, 1289, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1292, 1292, 1292, 1292, 1292, 1292, 1292, 1292,
        1293, 1293, 1293, 1293, 1293, 1293, 1293, 1293, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1296, 1297, 1, 1, 1, 1, 1, 1,
        1298, 1298, 1298, 1298, 1298, 1298, 1298, 1298, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1301, 1301, 1301, 1301, 1301, 1301, 1301, 1301,
        1302, 1302, 1302, 1302, 1302, 1302, 1302, 1302, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1305, 1306, 1, 1, 1, 1, 1, 1,
        1307, 1307, 1307, 1307, 1307, 1307, 1307, 1307, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1310, 1310, 1310, 1310, 1310, 1310, 1310, 1310,
        1311, 1311, 1311, 1311, 1311, 1311, 1311, 1311, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1314, 1315, 1316, 1317, 1318, 1, 1, 1,
        1228, 1228, 1228, 1228, 1228, 1228, 1228, 1228, 1, 1, 1, 1, 1, 1, 1, 1, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1230, 1230, 1230, 1230, 1230, 1230, 1230, 1231,
        1232, 1232, 1232, 1232, 1232, 1232, 1232, 1233, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1236, 1237, 1238, 1239, 1240, 1, 1, 1,
        1241, 1241, 1241, 1241, 1241, 1241, 1241, 1241, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1244, 1244, 1244, 1244, 1244, 1244, 1244, 1244,
        1245, 1245, 1245, 1245, 1245, 1245, 1245, 1245, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1248, 1249, 1250, 1251, 1252, 1, 1, 1,
        1253, 1253, 1253, 1253, 1253, 1253, 1253, 1253, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1256, 1256, 1256, 1256, 1256, 1256, 1256, 1256,
        1257, 1257, 1257, 1257, 1257, 1257, 1257, 1257, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1260, 1261, 1262, 1263, 1264, 1, 1, 1,
        1265, 1265, 1265, 1265, 1265, 1265, 1265, 1265, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1268, 1268, 1268, 1268, 1268, 1268, 1268, 1268,
        1269, 1269, 1269, 1269, 1269, 1269, 1269, 1269, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1272, 1273, 1274, 1275, 1276, 1, 1, 1,
        1277, 1277, 1277, 1277, 1277, 1277, 1277, 1277, 1278, 1278, 1278, 1278, 1278, 1278, 1278, 1279, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1281, 1281, 1281, 1281, 1281, 1281, 1281, 1282,
        1283, 1283, 1283, 1283, 1283, 1283, 1283, 1284, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1287, 1288, 1, 1, 1, 1, 1, 1,
        1289, 1289, 1289, 1289, 1289, 1289, 1289, 1289, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1292, 1292, 1292, 1292, 1292, 1292, 1292, 1292,
        1293, 1293, 1293, 1293, 1293, 1293, 1293, 1293, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1296, 1297, 1, 1, 1, 1, 1, 1,
        1298, 1298, 1298, 1298, 1298, 1298, 1298, 1298, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1301, 1301, 1301, 1301, 1301, 1301, 1301, 1301,
        1302, 1302, 1302, 1302, 1302, 1302, 1302, 1302, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1305, 1306, 1, 1, 1, 1, 1, 1,
        1307, 1307, 1307, 1307, 1307, 1307, 1307, 1307, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1310, 1310, 1310, 1310, 1310, 1310, 1310, 1310,
        1311, 1311, 1311, 1311, 1311, 1311, 1311, 1311, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1314, 1315, 1316, 1317, 1318, 1, 1, 1,
        1228, 1228, 1228, 1228, 1228, 1228, 1228, 1228, 1, 1, 1, 1, 1, 1, 1, 1, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1229, 1230, 1230, 1230, 1230, 1230, 1230, 1230, 1231,
        1232, 1232, 1232, 1232, 1232, 1232, 1232, 1233, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1234, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1235, 1236, 1237, 1238, 1239, 1240, 1, 1, 1,
        1241, 1241, 1241, 1241, 1241, 1241, 1241, 1241, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1242, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1243, 1244, 1244, 1244, 1244, 1244, 1244, 1244, 1244,
        1245, 1245, 1245, 1245, 1245, 1245, 1245, 1245, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1246, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1247, 1248, 1249, 1250, 1251, 1252, 1, 1, 1,
        1253, 1253, 1253, 1253, 1253, 1253, 1253, 1253, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1254, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1255, 1256, 1256, 1256, 1256, 1256, 1256, 1256, 1256,
        1257, 1257, 1257, 1257, 1257, 1257, 1257, 1257, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1258, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1259, 1260, 1261, 1262, 1263, 1264, 1, 1, 1,
        1265, 1265, 1265, 1265, 1265, 1265, 1265, 1265, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1266, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1267, 1268, 1268, 1268, 1268, 1268, 1268, 1268, 1268,
        1269, 1269, 1269, 1269, 1269, 1269, 1269, 1269, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1270, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1271, 1272, 1273, 1274, 1275, 1276, 1, 1, 1,
        1277, 1277, 1277, 1277, 1277, 1277, 1277, 1277, 1319, 1319, 1319, 1319, 1319, 1319, 1319, 1320, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1280, 1281, 1281, 1281, 1281, 1281, 1281, 1281, 1282,
        1283, 1283, 1283, 1283, 1283, 1283, 1283, 1284, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1285, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1286, 1287, 1288, 1, 1, 1, 1, 1, 1,
        1289, 1289, 1289, 1289, 1289, 1289, 1289, 1289, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1290, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1291, 1292, 1292, 1292, 1292, 1292, 1292, 1292, 1292,
        1293, 1293, 1293, 1293, 1293, 1293, 1293, 1293, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1294, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1295, 1296, 1297, 1, 1, 1, 1, 1, 1,
        1298, 1298, 1298, 1298, 1298, 1298, 1298, 1298, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1299, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1301, 1301, 1301, 1301, 1301, 1301, 1301, 1301,
        1302, 1302, 1302, 1302, 1302, 1302, 1302, 1302, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1303, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1304, 1305, 1306, 1, 1, 1, 1, 1, 1,
        1307, 1307, 1307, 1307, 1307, 1307, 1307, 1307, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1308, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1309, 1310, 1310, 1310, 1310, 1310, 1310, 1310, 1310,
        1311, 1311, 1311, 1311, 1311, 1311, 1311, 1311, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1312, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1313, 1314, 1315, 1316, 1317, 1318, 1, 1, 1,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321, 1321,
        1322, 1322, 1322, 1322, 1322, 1322, 1322, 1322, 1, 1, 1, 1, 1, 1, 1, 1, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1324, 1324, 1324, 1324, 1324, 1324, 1324, 1325,
        1326, 1326, 1326, 1326, 1326, 1326, 1326, 1327, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1330, 1331, 1332, 1333, 1334, 1, 1, 1,
        1335, 1335, 1335, 1335, 1335, 1335, 1335, 1335, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1338, 1338, 1338, 1338, 1338, 1338, 1338, 1338,
        1339, 1339, 1339, 1339, 1339, 1339, 1339, 1339, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1342, 1343, 1344, 1345, 1346, 1, 1, 1,
        1347, 1347, 1347, 1347, 1347, 1347, 1347, 1347, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1350, 1350, 1350, 1350, 1350, 1350, 1350, 1350,
        1351, 1351, 1351, 1351, 1351, 1351, 1351, 1351, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1354, 1355, 1356, 1357, 1358, 1, 1, 1,
        1359, 1359, 1359, 1359, 1359, 1359, 1359, 1359, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1362, 1362, 1362, 1362, 1362, 1362, 1362, 1362,
        1363, 1363, 1363, 1363, 1363, 1363, 1363, 1363, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1366, 1367, 1368, 1369, 1370, 1, 1, 1,
        1371, 1371, 1371, 1371, 1371, 1371, 1371, 1371, 1372, 1372, 1372, 1372, 1372, 1372, 1372, 1373, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1375, 1375, 1375, 1375, 1375, 1375, 1375, 1376,
        1377, 1377, 1377, 1377, 1377, 1377, 1377, 1378, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1381, 1382, 1, 1, 1, 1, 1, 1,
        1383, 1383, 1383, 1383, 1383, 1383, 1383, 1383, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1386, 1386, 1386, 1386, 1386, 1386, 1386, 1386,
        1387, 1387, 1387, 1387, 1387, 1387, 1387, 1387, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1390, 1391, 1, 1, 1, 1, 1, 1,
        1392, 1392, 1392, 1392, 1392, 1392, 1392, 1392, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1395, 1395, 1395, 1395, 1395, 1395, 1395, 1395,
        1396, 1396, 1396, 1396, 1396, 1396, 1396, 1396, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1399, 1400, 1, 1, 1, 1, 1, 1,
        1401, 1401, 1401, 1401, 1401, 1401, 1401, 1401, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1404, 1404, 1404, 1404, 1404, 1404, 1404, 1404,
        1405, 1405, 1405, 1405, 1405, 1405, 1405, 1405, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1408, 1409, 1410, 1411, 1412, 1, 1, 1,
        1322, 1322, 1322, 1322, 1322, 1322, 1322, 1322, 1, 1, 1, 1, 1, 1, 1, 1, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1324, 1324, 1324, 1324, 1324, 1324, 1324, 1325,
        1326, 1326, 1326, 1326, 1326, 1326, 1326, 1327, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1330, 1331, 1332, 1333, 1334, 1, 1, 1,
        1335, 1335, 1335, 1335, 1335, 1335, 1335, 1335, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1338, 1338, 1338, 1338, 1338, 1338, 1338, 1338,
        1339, 1339, 1339, 1339, 1339, 1339, 1339, 1339, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1342, 1343, 1344, 1345, 1346, 1, 1, 1,
        1347, 1347, 1347, 1347, 1347, 1347, 1347, 1347, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1350, 1350, 1350, 1350, 1350, 1350, 1350, 1350,
        1351, 1351, 1351, 1351, 1351, 1351, 1351, 1351, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1354, 1355, 1356, 1357, 1358, 1, 1, 1,
        1359, 1359, 1359, 1359, 1359, 1359, 1359, 1359, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1362, 1362, 1362, 1362, 1362, 1362, 1362, 1362,
        1363, 1363, 1363, 1363, 1363, 1363, 1363, 1363, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1366, 1367, 1368, 1369, 1370, 1, 1, 1,
        1371, 1371, 1371, 1371, 1371, 1371, 1371, 1371, 1372, 1372, 1372, 1372, 1372, 1372, 1372, 1373, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1375, 1375, 1375, 1375, 1375, 1375, 1375, 1376,
        1377, 1377, 1377, 1377, 1377, 1377, 1377, 1378, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1381, 1382, 1, 1, 1, 1, 1, 1,
        1383, 1383, 1383, 1383, 1383, 1383, 1383, 1383, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1386, 1386, 1386, 1386, 1386, 1386, 1386, 1386,
        1387, 1387, 1387, 1387, 1387, 1387, 1387, 1387, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1390, 1391, 1, 1, 1, 1, 1, 1,
        1392, 1392, 1392, 1392, 1392, 1392, 1392, 1392, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1395, 1395, 1395, 1395, 1395, 1395, 1395, 1395,
        1396, 1396, 1396, 1396, 1396, 1396, 1396, 1396, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1399, 1400, 1, 1, 1, 1, 1, 1,
        1401, 1401, 1401, 1401, 1401, 1401, 1401, 1401, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1404, 1404, 1404, 1404, 1404, 1404, 1404, 1404,
        1405, 1405, 1405, 1405, 1405, 1405, 1405, 1405, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1408, 1409, 1410, 1411, 1412, 1, 1, 1,
        1322, 1322, 1322, 1322, 1322, 1322, 1322, 1322, 1, 1, 1, 1, 1, 1, 1, 1, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1324, 1324, 1324, 1324, 1324, 1324, 1324, 1325,
        1326, 1326, 1326, 1326, 1326, 1326, 1326, 1327, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1330, 1331, 1332, 1333, 1334, 1, 1, 1,
        1335, 1335, 1335, 1335, 1335, 1335, 1335, 1335, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1338, 1338, 1338, 1338, 1338, 1338, 1338, 1338,
        1339, 1339, 1339, 1339, 1339, 1339, 1339, 1339, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1342, 1343, 1344, 1345, 1346, 1, 1, 1,
        1347, 1347, 1347, 1347, 1347, 1347, 1347, 1347, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1350, 1350, 1350, 1350, 1350, 1350, 1350, 1350,
        1351, 1351, 1351, 1351, 1351, 1351, 1351, 1351, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1354, 1355, 1356, 1357, 1358, 1, 1, 1,
        1359, 1359, 1359, 1359, 1359, 1359, 1359, 1359, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1362, 1362, 1362, 1362, 1362, 1362, 1362, 1362,
        1363, 1363, 1363, 1363, 1363, 1363, 1363, 1363, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1366, 1367, 1368, 1369, 1370, 1, 1, 1,
        1371, 1371, 1371, 1371, 1371, 1371, 1371, 1371, 1372, 1372, 1372, 1372, 1372, 1372, 1372, 1373, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1375, 1375, 1375, 1375, 1375, 1375, 1375, 1376,
        1377, 1377, 1377, 1377, 1377, 1377, 1377, 1378, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1381, 1382, 1, 1, 1, 1, 1, 1,
        1383, 1383, 1383, 1383, 1383, 1383, 1383, 1383, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1386, 1386, 1386, 1386, 1386, 1386, 1386, 1386,
        1387, 1387, 1387, 1387, 1387, 1387, 1387, 1387, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1390, 1391, 1, 1, 1, 1, 1, 1,
        1392, 1392, 1392, 1392, 1392, 1392, 1392, 1392, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1395, 1395, 1395, 1395, 1395, 1395, 1395, 1395,
        1396, 1396, 1396, 1396, 1396, 1396, 1396, 1396, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1399, 1400, 1, 1, 1, 1, 1, 1,
        1401, 1401, 1401, 1401, 1401, 1401, 1401, 1401, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1404, 1404, 1404, 1404, 1404, 1404, 1404, 1404,
        1405, 1405, 1405, 1405, 1405, 1405, 1405, 1405, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1408, 1409, 1410, 1411, 1412, 1, 1, 1,
        1322, 1322, 1322, 1322, 1322, 1322, 1322, 1322, 1, 1, 1, 1, 1, 1, 1, 1, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1324, 1324, 1324, 1324, 1324, 1324, 1324, 1325,
        1326, 1326, 1326, 1326, 1326, 1326, 1326, 1327, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1330, 1331, 1332, 1333, 1334, 1, 1, 1,
        1335, 1335, 1335, 1335, 1335, 1335, 1335, 1335, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1338, 1338, 1338, 1338, 1338, 1338, 1338, 1338,
        1339, 1339, 1339, 1339, 1339, 1339, 1339, 1339, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1342, 1343, 1344, 1345, 1346, 1, 1, 1,
        1347, 1347, 1347, 1347, 1347, 1347, 1347, 1347, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1350, 1350, 1350, 1350, 1350, 1350, 1350, 1350,
        1351, 1351, 1351, 1351, 1351, 1351, 1351, 1351, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1354, 1355, 1356, 1357, 1358, 1, 1, 1,
        1359, 1359, 1359, 1359, 1359, 1359, 1359, 1359, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1362, 1362, 1362, 1362, 1362, 1362, 1362, 1362,
        1363, 1363, 1363, 1363, 1363, 1363, 1363, 1363, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1366, 1367, 1368, 1369, 1370, 1, 1, 1,
        1371, 1371, 1371, 1371, 1371, 1371, 1371, 1371, 1372, 1372, 1372, 1372, 1372, 1372, 1372, 1373, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1375, 1375, 1375, 1375, 1375, 1375, 1375, 1376,
        1377, 1377, 1377, 1377, 1377, 1377, 1377, 1378, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1381, 1382, 1, 1, 1, 1, 1, 1,
        1383, 1383, 1383, 1383, 1383, 1383, 1383, 1383, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1386, 1386, 1386, 1386, 1386, 1386, 1386, 1386,
        1387, 1387, 1387, 1387, 1387, 1387, 1387, 1387, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1390, 1391, 1, 1, 1, 1, 1, 1,
        1392, 1392, 1392, 1392, 1392, 1392, 1392, 1392, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1395, 1395, 1395, 1395, 1395, 1395, 1395, 1395,
        1396, 1396, 1396, 1396, 1396, 1396, 1396, 1396, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1399, 1400, 1, 1, 1, 1, 1, 1,
        1401, 1401, 1401, 1401, 1401, 1401, 1401, 1401, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1404, 1404, 1404, 1404, 1404, 1404, 1404, 1404,
        1405, 1405, 1405, 1405, 1405, 1405, 1405, 1405, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1408, 1409, 1410, 1411, 1412, 1, 1, 1,
        1322, 1322, 1322, 1322, 1322, 1322, 1322, 1322, 1, 1, 1, 1, 1, 1, 1, 1, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1324, 1324, 1324, 1324, 1324, 1324, 1324, 1325,
        1326, 1326, 1326, 1326, 1326, 1326, 1326, 1327, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1330, 1331, 1332, 1333, 1334, 1, 1, 1,
        1335, 1335, 1335, 1335, 1335, 1335, 1335, 1335, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1338, 1338, 1338, 1338, 1338, 1338, 1338, 1338,
        1339, 1339, 1339, 1339, 1339, 1339, 1339, 1339, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1342, 1343, 1344, 1345, 1346, 1, 1, 1,
        1347, 1347, 1347, 1347, 1347, 1347, 1347, 1347, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1350, 1350, 1350, 1350, 1350, 1350, 1350, 1350,
        1351, 1351, 1351, 1351, 1351, 1351, 1351, 1351, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1354, 1355, 1356, 1357, 1358, 1, 1, 1,
        1359, 1359, 1359, 1359, 1359, 1359, 1359, 1359, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1362, 1362, 1362, 1362, 1362, 1362, 1362, 1362,
        1363, 1363, 1363, 1363, 1363, 1363, 1363, 1363, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1366, 1367, 1368, 1369, 1370, 1, 1, 1,
        1371, 1371, 1371, 1371, 1371, 1371, 1371, 1371, 1372, 1372, 1372, 1372, 1372, 1372, 1372, 1373, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1375, 1375, 1375, 1375, 1375, 1375, 1375, 1376,
        1377, 1377, 1377, 1377, 1377, 1377, 1377, 1378, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1381, 1382, 1, 1, 1, 1, 1, 1,
        1383, 1383, 1383, 1383, 1383, 1383, 1383, 1383, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1386, 1386, 1386, 1386, 1386, 1386, 1386, 1386,
        1387, 1387, 1387, 1387, 1387, 1387, 1387, 1387, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1390, 1391, 1, 1, 1, 1, 1, 1,
        1392, 1392, 1392, 1392, 1392, 1392, 1392, 1392, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1395, 1395, 1395, 1395, 1395, 1395, 1395, 1395,
        1396, 1396, 1396, 1396, 1396, 1396, 1396, 1396, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1399, 1400, 1, 1, 1, 1, 1, 1,
        1401, 1401, 1401, 1401, 1401, 1401, 1401, 1401, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1404, 1404, 1404, 1404, 1404, 1404, 1404, 1404,
        1405, 1405, 1405, 1405, 1405, 1405, 1405, 1405, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1408, 1409, 1410, 1411, 1412, 1, 1, 1,
        1322, 1322, 1322, 1322, 1322, 1322, 1322, 1322, 1, 1, 1, 1, 1, 1, 1, 1, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1324, 1324, 1324, 1324, 1324, 1324, 1324, 1325,
        1326, 1326, 1326, 1326, 1326, 1326, 1326, 1327, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1330, 1331, 1332, 1333, 1334, 1, 1, 1,
        1335, 1335, 1335, 1335, 1335, 1335, 1335, 1335, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1338, 1338, 1338, 1338, 1338, 1338, 1338, 1338,
        1339, 1339, 1339, 1339, 1339, 1339, 1339, 1339, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1342, 1343, 1344, 1345, 1346, 1, 1, 1,
        1347, 1347, 1347, 1347, 1347, 1347, 1347, 1347, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1350, 1350, 1350, 1350, 1350, 1350, 1350, 1350,
        1351, 1351, 1351, 1351, 1351, 1351, 1351, 1351, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1354, 1355, 1356, 1357, 1358, 1, 1, 1,
        1359, 1359, 1359, 1359, 1359, 1359, 1359, 1359, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1362, 1362, 1362, 1362, 1362, 1362, 1362, 1362,
        1363, 1363, 1363, 1363, 1363, 1363, 1363, 1363, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1366, 1367, 1368, 1369, 1370, 1, 1, 1,
        1371, 1371, 1371, 1371, 1371, 1371, 1371, 1371, 1372, 1372, 1372, 1372, 1372, 1372, 1372, 1373, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1375, 1375, 1375, 1375, 1375, 1375, 1375, 1376,
        1377, 1377, 1377, 1377, 1377, 1377, 1377, 1378, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1381, 1382, 1, 1, 1, 1, 1, 1,
        1383, 1383, 1383, 1383, 1383, 1383, 1383, 1383, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1386, 1386, 1386, 1386, 1386, 1386, 1386, 1386,
        1387, 1387, 1387, 1387, 1387, 1387, 1387, 1387, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1390, 1391, 1, 1, 1, 1, 1, 1,
        1392, 1392, 1392, 1392, 1392, 1392, 1392, 1392, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1395, 1395, 1395, 1395, 1395, 1395, 1395, 1395,
        1396, 1396, 1396, 1396, 1396, 1396, 1396, 1396, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1399, 1400, 1, 1, 1, 1, 1, 1,
        1401, 1401, 1401, 1401, 1401, 1401, 1401, 1401, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1404, 1404, 1404, 1404, 1404, 1404, 1404, 1404,
        1405, 1405, 1405, 1405, 1405, 1405, 1405, 1405, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1408, 1409, 1410, 1411, 1412, 1, 1, 1,
        1322, 1322, 1322, 1322, 1322, 1322, 1322, 1322, 1, 1, 1, 1, 1, 1, 1, 1, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1324, 1324, 1324, 1324, 1324, 1324, 1324, 1325,
        1326, 1326, 1326, 1326, 1326, 1326, 1326, 1327, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1330, 1331, 1332, 1333, 1334, 1, 1, 1,
        1335, 1335, 1335, 1335, 1335, 1335, 1335, 1335, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1338, 1338, 1338, 1338, 1338, 1338, 1338, 1338,
        1339, 1339, 1339, 1339, 1339, 1339, 1339, 1339, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1342, 1343, 1344, 1345, 1346, 1, 1, 1,
        1347, 1347, 1347, 1347, 1347, 1347, 1347, 1347, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1350, 1350, 1350, 1350, 1350, 1350, 1350, 1350,
        1351, 1351, 1351, 1351, 1351, 1351, 1351, 1351, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1354, 1355, 1356, 1357, 1358, 1, 1, 1,
        1359, 1359, 1359, 1359, 1359, 1359, 1359, 1359, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1362, 1362, 1362, 1362, 1362, 1362, 1362, 1362,
        1363, 1363, 1363, 1363, 1363, 1363, 1363, 1363, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1366, 1367, 1368, 1369, 1370, 1, 1, 1,
        1371, 1371, 1371, 1371, 1371, 1371, 1371, 1371, 1372, 1372, 1372, 1372, 1372, 1372, 1372, 1373, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1375, 1375, 1375, 1375, 1375, 1375, 1375, 1376,
        1377, 1377, 1377, 1377, 1377, 1377, 1377, 1378, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1381, 1382, 1, 1, 1, 1, 1, 1,
        1383, 1383, 1383, 1383, 1383, 1383, 1383, 1383, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1386, 1386, 1386, 1386, 1386, 1386, 1386, 1386,
        1387, 1387, 1387, 1387, 1387, 1387, 1387, 1387, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1390, 1391, 1, 1, 1, 1, 1, 1,
        1392, 1392, 1392, 1392, 1392, 1392, 1392, 1392, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1395, 1395, 1395, 1395, 1395, 1395, 1395, 1395,
        1396, 1396, 1396, 1396, 1396, 1396, 1396, 1396, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1399, 1400, 1, 1, 1, 1, 1, 1,
        1401, 1401, 1401, 1401, 1401, 1401, 1401, 1401, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1404, 1404, 1404, 1404, 1404, 1404, 1404, 1404,
        1405, 1405, 1405, 1405, 1405, 1405, 1405, 1405, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1408, 1409, 1410, 1411, 1412, 1, 1, 1,
        1322, 1322, 1322, 1322, 1322, 1322, 1322, 1322, 1, 1, 1, 1, 1, 1, 1, 1, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1323, 1324, 1324, 1324, 1324, 1324, 1324, 1324, 1325,
        1326, 1326, 1326, 1326, 1326, 1326, 1326, 1327, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1328, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1329, 1330, 1331, 1332, 1333, 1334, 1, 1, 1,
        1335, 1335, 1335, 1335, 1335, 1335, 1335, 1335, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1336, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1337, 1338, 1338, 1338, 1338, 1338, 1338, 1338, 1338,
        1339, 1339, 1339, 1339, 1339, 1339, 1339, 1339, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1340, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1341, 1342, 1343, 1344, 1345, 1346, 1, 1, 1,
        1347, 1347, 1347, 1347, 1347, 1347, 1347, 1347, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1348, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1349, 1350, 1350, 1350, 1350, 1350, 1350, 1350, 1350,
        1351, 1351, 1351, 1351, 1351, 1351, 1351, 1351, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1352, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1353, 1354, 1355, 1356, 1357, 1358, 1, 1, 1,
        1359, 1359, 1359, 1359, 1359, 1359, 1359, 1359, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1360, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1361, 1362, 1362, 1362, 1362, 1362, 1362, 1362, 1362,
        1363, 1363, 1363, 1363, 1363, 1363, 1363, 1363, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1364, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1365, 1366, 1367, 1368, 1369, 1370, 1, 1, 1,
        1371, 1371, 1371, 1371, 1371, 1371, 1371, 1371, 1413, 1413, 1413, 1413, 1413, 1413, 1413, 1414, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1374, 1375, 1375, 1375, 1375, 1375, 1375, 1375, 1376,
        1377, 1377, 1377, 1377, 1377, 1377, 1377, 1378, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1379, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1380, 1381, 1382, 1, 1, 1, 1, 1, 1,
        1383, 1383, 1383, 1383, 1383, 1383, 1383, 1383, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1384, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1385, 1386, 1386, 1386, 1386, 1386, 1386, 1386, 1386,
        1387, 1387, 1387, 1387, 1387, 1387, 1387, 1387, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1388, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1389, 1390, 1391, 1, 1, 1, 1, 1, 1,
        1392, 1392, 1392, 1392, 1392, 1392, 1392, 1392, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1393, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1394, 1395, 1395, 1395, 1395, 1395, 1395, 1395, 1395,
        1396, 1396, 1396, 1396, 1396, 1396, 1396, 1396, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1397, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1398, 1399, 1400, 1, 1, 1, 1, 1, 1,
        1401, 1401, 1401, 1401, 1401, 1401, 1401, 1401, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1402, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1403, 1404, 1404, 1404, 1404, 1404, 1404, 1404, 1404,
        1405, 1405, 1405, 1405, 1405, 1405, 1405, 1405, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1406, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1407, 1408, 1409, 1410, 1411, 1412, 1, 1, 1,
        1415, 1415, 1415, 1415, 1415, 1415, 1415, 1415, 1, 1, 1, 1, 1, 1, 1, 1, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1417, 1417, 1417, 1417, 1417, 1417, 1417, 1418,
        1419, 1419, 1419, 1419, 1419, 1419, 1419, 1420, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1423, 1424, 1425, 1426, 1427, 1, 1, 1,
        1428, 1428, 1428, 1428, 1428, 1428, 1428, 1428, 1, 1, 1, 1, 1, 1, 1, 1, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1430, 1430, 1430, 1430, 1430, 1430, 1430, 1430,
        1431, 1431, 1431, 1431, 1431, 1431, 1431, 1431, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1434, 1435, 1436, 1437, 1438, 1, 1, 1,
        1439, 1439, 1439, 1439, 1439, 1439, 1439, 1439, 1, 1, 1, 1, 1, 1, 1, 1, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1441, 1441, 1441, 1441, 1441, 1441, 1441, 1441,
        1442, 1442, 1442, 1442, 1442, 1442, 1442, 1442, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1445, 1446, 1447, 1448, 1449, 1, 1, 1,
        1450, 1450, 1450, 1450, 1450, 1450, 1450, 1450, 1, 1, 1, 1, 1, 1, 1, 1, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1452, 1452, 1452, 1452, 1452, 1452, 1452, 1452,
        1453, 1453, 1453, 1453, 1453, 1453, 1453, 1453, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1456, 1457, 1458, 1459, 1460, 1, 1, 1,
        1461, 1461, 1461, 1461, 1461, 1461, 1461, 1461, 1462, 1462, 1462, 1462, 1462, 1462, 1462, 1463, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1465, 1465, 1465, 1465, 1465, 1465, 1465, 1466,
        1467, 1467, 1467, 1467, 1467, 1467, 1467, 1468, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1471, 1472, 1, 1, 1, 1, 1, 1,
        1473, 1473, 1473, 1473, 1473, 1473, 1473, 1473, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1476, 1476, 1476, 1476, 1476, 1476, 1476, 1476,
        1477, 1477, 1477, 1477, 1477, 1477, 1477, 1477, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1480, 1481, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1484, 1484, 1484, 1484, 1484, 1484, 1484, 1484,
        1485, 1485, 1485, 1485, 1485, 1485, 1485, 1485, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1488, 1489, 1, 1, 1, 1, 1, 1,
        1490, 1490, 1490, 1490, 1490, 1490, 1490, 1490, 1, 1, 1, 1, 1, 1, 1, 1, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1492, 1492, 1492, 1492, 1492, 1492, 1492, 1492,
        1493, 1493, 1493, 1493, 1493, 1493, 1493, 1493, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1496, 1497, 1498, 1499, 1500, 1, 1, 1,
        1415, 1415, 1415, 1415, 1415, 1415, 1415, 1415, 1, 1, 1, 1, 1, 1, 1, 1, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1417, 1417, 1417, 1417, 1417, 1417, 1417, 1418,
        1419, 1419, 1419, 1419, 1419, 1419, 1419, 1420, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1423, 1424, 1425, 1426, 1427, 1, 1, 1,
        1428, 1428, 1428, 1428, 1428, 1428, 1428, 1428, 1, 1, 1, 1, 1, 1, 1, 1, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1430, 1430, 1430, 1430, 1430, 1430, 1430, 1430,
        1431, 1431, 1431, 1431, 1431, 1431, 1431, 1431, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1434, 1435, 1436, 1437, 1438, 1, 1, 1,
        1439, 1439, 1439, 1439, 1439, 1439, 1439, 1439, 1, 1, 1, 1, 1, 1, 1, 1, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1441, 1441, 1441, 1441, 1441, 1441, 1441, 1441,
        1442, 1442, 1442, 1442, 1442, 1442, 1442, 1442, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1445, 1446, 1447, 1448, 1449, 1, 1, 1,
        1450, 1450, 1450, 1450, 1450, 1450, 1450, 1450, 1, 1, 1, 1, 1, 1, 1, 1, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1452, 1452, 1452, 1452, 1452, 1452, 1452, 1452,
        1453, 1453, 1453, 1453, 1453, 1453, 1453, 1453, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1456, 1457, 1458, 1459, 1460, 1, 1, 1,
        1461, 1461, 1461, 1461, 1461, 1461, 1461, 1461, 1462, 1462, 1462, 1462, 1462, 1462, 1462, 1463, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1465, 1465, 1465, 1465, 1465, 1465, 1465, 1466,
        1467, 1467, 1467, 1467, 1467, 1467, 1467, 1468, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1471, 1472, 1, 1, 1, 1, 1, 1,
        1473, 1473, 1473, 1473, 1473, 1473, 1473, 1473, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1476, 1476, 1476, 1476, 1476, 1476, 1476, 1476,
        1477, 1477, 1477, 1477, 1477, 1477, 1477, 1477, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1480, 1481, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1484, 1484, 1484, 1484, 1484, 1484, 1484, 1484,
        1485, 1485, 1485, 1485, 1485, 1485, 1485, 1485, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1488, 1489, 1, 1, 1, 1, 1, 1,
        1490, 1490, 1490, 1490, 1490, 1490, 1490, 1490, 1, 1, 1, 1, 1, 1, 1, 1, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1492, 1492, 1492, 1492, 1492, 1492, 1492, 1492,
        1493, 1493, 1493, 1493, 1493, 1493, 1493, 1493, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1496, 1497, 1498, 1499, 1500, 1, 1, 1,
        1415, 1415, 1415, 1415, 1415, 1415, 1415, 1415, 1, 1, 1, 1, 1, 1, 1, 1, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1417, 1417, 1417, 1417, 1417, 1417, 1417, 1418,
        1419, 1419, 1419, 1419, 1419, 1419, 1419, 1420, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1423, 1424, 1425, 1426, 1427, 1, 1, 1,
        1428, 1428, 1428, 1428, 1428, 1428, 1428, 1428, 1, 1, 1, 1, 1, 1, 1, 1, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1430, 1430, 1430, 1430, 1430, 1430, 1430, 1430,
        1431, 1431, 1431, 1431, 1431, 1431, 1431, 1431, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1434, 1435, 1436, 1437, 1438, 1, 1, 1,
        1439, 1439, 1439, 1439, 1439, 1439, 1439, 1439, 1, 1, 1, 1, 1, 1, 1, 1, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1441, 1441, 1441, 1441, 1441, 1441, 1441, 1441,
        1442, 1442, 1442, 1442, 1442, 1442, 1442, 1442, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1445, 1446, 1447, 1448, 1449, 1, 1, 1,
        1450, 1450, 1450, 1450, 1450, 1450, 1450, 1450, 1, 1, 1, 1, 1, 1, 1, 1, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1452, 1452, 1452, 1452, 1452, 1452, 1452, 1452,
        1453, 1453, 1453, 1453, 1453, 1453, 1453, 1453, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1456, 1457, 1458, 1459, 1460, 1, 1, 1,
        1461, 1461, 1461, 1461, 1461, 1461, 1461, 1461, 1462, 1462, 1462, 1462, 1462, 1462, 1462, 1463, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1465, 1465, 1465, 1465, 1465, 1465, 1465, 1466,
        1467, 1467, 1467, 1467, 1467, 1467, 1467, 1468, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1471, 1472, 1, 1, 1, 1, 1, 1,
        1473, 1473, 1473, 1473, 1473, 1473, 1473, 1473, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1476, 1476, 1476, 1476, 1476, 1476, 1476, 1476,
        1477, 1477, 1477, 1477, 1477, 1477, 1477, 1477, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1480, 1481, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1484, 1484, 1484, 1484, 1484, 1484, 1484, 1484,
        1485, 1485, 1485, 1485, 1485, 1485, 1485, 1485, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1488, 1489, 1, 1, 1, 1, 1, 1,
        1490, 1490, 1490, 1490, 1490, 1490, 1490, 1490, 1, 1, 1, 1, 1, 1, 1, 1, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1492, 1492, 1492, 1492, 1492, 1492, 1492, 1492,
        1493, 1493, 1493, 1493, 1493, 1493, 1493, 1493, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1496, 1497, 1498, 1499, 1500, 1, 1, 1,
        1415, 1415, 1415, 1415, 1415, 1415, 1415, 1415, 1, 1, 1, 1, 1, 1, 1, 1, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1417, 1417, 1417, 1417, 1417, 1417, 1417, 1418,
        1419, 1419, 1419, 1419, 1419, 1419, 1419, 1420, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1423, 1424, 1425, 1426, 1427, 1, 1, 1,
        1428, 1428, 1428, 1428, 1428, 1428, 1428, 1428, 1, 1, 1, 1, 1, 1, 1, 1, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1430, 1430, 1430, 1430, 1430, 1430, 1430, 1430,
        1431, 1431, 1431, 1431, 1431, 1431, 1431, 1431, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1434, 1435, 1436, 1437, 1438, 1, 1, 1,
        1439, 1439, 1439, 1439, 1439, 1439, 1439, 1439, 1, 1, 1, 1, 1, 1, 1, 1, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1441, 1441, 1441, 1441, 1441, 1441, 1441, 1441,
        1442, 1442, 1442, 1442, 1442, 1442, 1442, 1442, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1445, 1446, 1447, 1448, 1449, 1, 1, 1,
        1450, 1450, 1450, 1450, 1450, 1450, 1450, 1450, 1, 1, 1, 1, 1, 1, 1, 1, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1452, 1452, 1452, 1452, 1452, 1452, 1452, 1452,
        1453, 1453, 1453, 1453, 1453, 1453, 1453, 1453, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1456, 1457, 1458, 1459, 1460, 1, 1, 1,
        1461, 1461, 1461, 1461, 1461, 1461, 1461, 1461, 1462, 1462, 1462, 1462, 1462, 1462, 1462, 1463, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1465, 1465, 1465, 1465, 1465, 1465, 1465, 1466,
        1467, 1467, 1467, 1467, 1467, 1467, 1467, 1468, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1471, 1472, 1, 1, 1, 1, 1, 1,
        1473, 1473, 1473, 1473, 1473, 1473, 1473, 1473, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1476, 1476, 1476, 1476, 1476, 1476, 1476, 1476,
        1477, 1477, 1477, 1477, 1477, 1477, 1477, 1477, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1480, 1481, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1484, 1484, 1484, 1484, 1484, 1484, 1484, 1484,
        1485, 1485, 1485, 1485, 1485, 1485, 1485, 1485, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1488, 1489, 1, 1, 1, 1, 1, 1,
        1490, 1490, 1490, 1490, 1490, 1490, 1490, 1490, 1, 1, 1, 1, 1, 1, 1, 1, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1492, 1492, 1492, 1492, 1492, 1492, 1492, 1492,
        1493, 1493, 1493, 1493, 1493, 1493, 1493, 1493, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1496, 1497, 1498, 1499, 1500, 1, 1, 1,
        1415, 1415, 1415, 1415, 1415, 1415, 1415, 1415, 1, 1, 1, 1, 1, 1, 1, 1, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1417, 1417, 1417, 1417, 1417, 1417, 1417, 1418,
        1419, 1419, 1419, 1419, 1419, 1419, 1419, 1420, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1423, 1424, 1425, 1426, 1427, 1, 1, 1,
        1428, 1428, 1428, 1428, 1428, 1428, 1428, 1428, 1, 1, 1, 1, 1, 1, 1, 1, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1430, 1430, 1430, 1430, 1430, 1430, 1430, 1430,
        1431, 1431, 1431, 1431, 1431, 1431, 1431, 1431, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1434, 1435, 1436, 1437, 1438, 1, 1, 1,
        1439, 1439, 1439, 1439, 1439, 1439, 1439, 1439, 1, 1, 1, 1, 1, 1, 1, 1, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1441, 1441, 1441, 1441, 1441, 1441, 1441, 1441,
        1442, 1442, 1442, 1442, 1442, 1442, 1442, 1442, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1445, 1446, 1447, 1448, 1449, 1, 1, 1,
        1450, 1450, 1450, 1450, 1450, 1450, 1450, 1450, 1, 1, 1, 1, 1, 1, 1, 1, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1452, 1452, 1452, 1452, 1452, 1452, 1452, 1452,
        1453, 1453, 1453, 1453, 1453, 1453, 1453, 1453, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1456, 1457, 1458, 1459, 1460, 1, 1, 1,
        1461, 1461, 1461, 1461, 1461, 1461, 1461, 1461, 1462, 1462, 1462, 1462, 1462, 1462, 1462, 1463, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1465, 1465, 1465, 1465, 1465, 1465, 1465, 1466,
        1467, 1467, 1467, 1467, 1467, 1467, 1467, 1468, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1471, 1472, 1, 1, 1, 1, 1, 1,
        1473, 1473, 1473, 1473, 1473, 1473, 1473, 1473, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1476, 1476, 1476, 1476, 1476, 1476, 1476, 1476,
        1477, 1477, 1477, 1477, 1477, 1477, 1477, 1477, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1480, 1481, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1484, 1484, 1484, 1484, 1484, 1484, 1484, 1484,
        1485, 1485, 1485, 1485, 1485, 1485, 1485, 1485, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1488, 1489, 1, 1, 1, 1, 1, 1,
        1490, 1490, 1490, 1490, 1490, 1490, 1490, 1490, 1, 1, 1, 1, 1, 1, 1, 1, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1492, 1492, 1492, 1492, 1492, 1492, 1492, 1492,
        1493, 1493, 1493, 1493, 1493, 1493, 1493, 1493, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1496, 1497, 1498, 1499, 1500, 1, 1, 1,
        1415, 1415, 1415, 1415, 1415, 1415, 1415, 1415, 1, 1, 1, 1, 1, 1, 1, 1, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1417, 1417, 1417, 1417, 1417, 1417, 1417, 1418,
        1419, 1419, 1419, 1419, 1419, 1419, 1419, 1420, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1423, 1424, 1425, 1426, 1427, 1, 1, 1,
        1428, 1428, 1428, 1428, 1428, 1428, 1428, 1428, 1, 1, 1, 1, 1, 1, 1, 1, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1430, 1430, 1430, 1430, 1430, 1430, 1430, 1430,
        1431, 1431, 1431, 1431, 1431, 1431, 1431, 1431, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1434, 1435, 1436, 1437, 1438, 1, 1, 1,
        1439, 1439, 1439, 1439, 1439, 1439, 1439, 1439, 1, 1, 1, 1, 1, 1, 1, 1, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1441, 1441, 1441, 1441, 1441, 1441, 1441, 1441,
        1442, 1442, 1442, 1442, 1442, 1442, 1442, 1442, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1445, 1446, 1447, 1448, 1449, 1, 1, 1,
        1450, 1450, 1450, 1450, 1450, 1450, 1450, 1450, 1, 1, 1, 1, 1, 1, 1, 1, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1452, 1452, 1452, 1452, 1452, 1452, 1452, 1452,
        1453, 1453, 1453, 1453, 1453, 1453, 1453, 1453, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1456, 1457, 1458, 1459, 1460, 1, 1, 1,
        1461, 1461, 1461, 1461, 1461, 1461, 1461, 1461, 1462, 1462, 1462, 1462, 1462, 1462, 1462, 1463, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1465, 1465, 1465, 1465, 1465, 1465, 1465, 1466,
        1467, 1467, 1467, 1467, 1467, 1467, 1467, 1468, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1471, 1472, 1, 1, 1, 1, 1, 1,
        1473, 1473, 1473, 1473, 1473, 1473, 1473, 1473, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1476, 1476, 1476, 1476, 1476, 1476, 1476, 1476,
        1477, 1477, 1477, 1477, 1477, 1477, 1477, 1477, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1480, 1481, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1484, 1484, 1484, 1484, 1484, 1484, 1484, 1484,
        1485, 1485, 1485, 1485, 1485, 1485, 1485, 1485, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1488, 1489, 1, 1, 1, 1, 1, 1,
        1490, 1490, 1490, 1490, 1490, 1490, 1490, 1490, 1, 1, 1, 1, 1, 1, 1, 1, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1492, 1492, 1492, 1492, 1492, 1492, 1492, 1492,
        1493, 1493, 1493, 1493, 1493, 1493, 1493, 1493, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1496, 1497, 1498, 1499, 1500, 1, 1, 1,
        1415, 1415, 1415, 1415, 1415, 1415, 1415, 1415, 1, 1, 1, 1, 1, 1, 1, 1, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1417, 1417, 1417, 1417, 1417, 1417, 1417, 1418,
        1419, 1419, 1419, 1419, 1419, 1419, 1419, 1420, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1423, 1424, 1425, 1426, 1427, 1, 1, 1,
        1428, 1428, 1428, 1428, 1428, 1428, 1428, 1428, 1, 1, 1, 1, 1, 1, 1, 1, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1430, 1430, 1430, 1430, 1430, 1430, 1430, 1430,
        1431, 1431, 1431, 1431, 1431, 1431, 1431, 1431, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1434, 1435, 1436, 1437, 1438, 1, 1, 1,
        1439, 1439, 1439, 1439, 1439, 1439, 1439, 1439, 1, 1, 1, 1, 1, 1, 1, 1, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1441, 1441, 1441, 1441, 1441, 1441, 1441, 1441,
        1442, 1442, 1442, 1442, 1442, 1442, 1442, 1442, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1445, 1446, 1447, 1448, 1449, 1, 1, 1,
        1450, 1450, 1450, 1450, 1450, 1450, 1450, 1450, 1, 1, 1, 1, 1, 1, 1, 1, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1452, 1452, 1452, 1452, 1452, 1452, 1452, 1452,
        1453, 1453, 1453, 1453, 1453, 1453, 1453, 1453, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1456, 1457, 1458, 1459, 1460, 1, 1, 1,
        1461, 1461, 1461, 1461, 1461, 1461, 1461, 1461, 1462, 1462, 1462, 1462, 1462, 1462, 1462, 1463, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1465, 1465, 1465, 1465, 1465, 1465, 1465, 1466,
        1467, 1467, 1467, 1467, 1467, 1467, 1467, 1468, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1471, 1472, 1, 1, 1, 1, 1, 1,
        1473, 1473, 1473, 1473, 1473, 1473, 1473, 1473, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1476, 1476, 1476, 1476, 1476, 1476, 1476, 1476,
        1477, 1477, 1477, 1477, 1477, 1477, 1477, 1477, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1480, 1481, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1484, 1484, 1484, 1484, 1484, 1484, 1484, 1484,
        1485, 1485, 1485, 1485, 1485, 1485, 1485, 1485, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1488, 1489, 1, 1, 1, 1, 1, 1,
        1490, 1490, 1490, 1490, 1490, 1490, 1490, 1490, 1, 1, 1, 1, 1, 1, 1, 1, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1492, 1492, 1492, 1492, 1492, 1492, 1492, 1492,
        1493, 1493, 1493, 1493, 1493, 1493, 1493, 1493, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1496, 1497, 1498, 1499, 1500, 1, 1, 1,
        1415, 1415, 1415, 1415, 1415, 1415, 1415, 1415, 1, 1, 1, 1, 1, 1, 1, 1, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1416, 1417, 1417, 1417, 1417, 1417, 1417, 1417, 1418,
        1419, 1419, 1419, 1419, 1419, 1419, 1419, 1420, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1421, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1422, 1423, 1424, 1425, 1426, 1427, 1, 1, 1,
        1428, 1428, 1428, 1428, 1428, 1428, 1428, 1428, 1, 1, 1, 1, 1, 1, 1, 1, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1429, 1430, 1430, 1430, 1430, 1430, 1430, 1430, 1430,
        1431, 1431, 1431, 1431, 1431, 1431, 1431, 1431, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1432, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1433, 1434, 1435, 1436, 1437, 1438, 1, 1, 1,
        1439, 1439, 1439, 1439, 1439, 1439, 1439, 1439, 1, 1, 1, 1, 1, 1, 1, 1, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1440, 1441, 1441, 1441, 1441, 1441, 1441, 1441, 1441,
        1442, 1442, 1442, 1442, 1442, 1442, 1442, 1442, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1443, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1444, 1445, 1446, 1447, 1448, 1449, 1, 1, 1,
        1450, 1450, 1450, 1450, 1450, 1450, 1450, 1450, 1, 1, 1, 1, 1, 1, 1, 1, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1451, 1452, 1452, 1452, 1452, 1452, 1452, 1452, 1452,
        1453, 1453, 1453, 1453, 1453, 1453, 1453, 1453, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1454, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1455, 1456, 1457, 1458, 1459, 1460, 1, 1, 1,
        1461, 1461, 1461, 1461, 1461, 1461, 1461, 1461, 1501, 1501, 1501, 1501, 1501, 1501, 1501, 1502, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1464, 1465, 1465, 1465, 1465, 1465, 1465, 1465, 1466,
        1467, 1467, 1467, 1467, 1467, 1467, 1467, 1468, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1469, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1470, 1471, 1472, 1, 1, 1, 1, 1, 1,
        1473, 1473, 1473, 1473, 1473, 1473, 1473, 1473, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1474, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1475, 1476, 1476, 1476, 1476, 1476, 1476, 1476, 1476,
        1477, 1477, 1477, 1477, 1477, 1477, 1477, 1477, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1478, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1479, 1480, 1481, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1482, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1483, 1484, 1484, 1484, 1484, 1484, 1484, 1484, 1484,
        1485, 1485, 1485, 1485, 1485, 1485, 1485, 1485, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1486, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1487, 1488, 1489, 1, 1, 1, 1, 1, 1,
        1490, 1490, 1490, 1490, 1490, 1490, 1490, 1490, 1, 1, 1, 1, 1, 1, 1, 1, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1491, 1492, 1492, 1492, 1492, 1492, 1492, 1492, 1492,
        1493, 1493, 1493, 1493, 1493, 1493, 1493, 1493, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1494, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1495, 1496, 1497, 1498, 1499, 1500, 1, 1, 1,
        1503, 1503, 1503, 1503, 1503, 1503, 1503, 1503, 1, 1, 1, 1, 1, 1, 1, 1, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1505, 1505, 1505, 1505, 1505, 1505, 1505, 1506,
        1507, 1507, 1507, 1507, 1507, 1507, 1507, 1508, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1511, 1512, 1513, 1514, 1515, 1, 1, 1,
        1516, 1516, 1516, 1516, 1516, 1516, 1516, 1516, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1519, 1519, 1519, 1519, 1519, 1519, 1519, 1519,
        1520, 1520, 1520, 1520, 1520, 1520, 1520, 1520, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1523, 1524, 1525, 1526, 1527, 1, 1, 1,
        1528, 1528, 1528, 1528, 1528, 1528, 1528, 1528, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1531, 1531, 1531, 1531, 1531, 1531, 1531, 1531,
        1532, 1532, 1532, 1532, 1532, 1532, 1532, 1532, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1535, 1536, 1537, 1538, 1539, 1, 1, 1,
        1540, 1540, 1540, 1540, 1540, 1540, 1540, 1540, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1543, 1543, 1543, 1543, 1543, 1543, 1543, 1543,
        1544, 1544, 1544, 1544, 1544, 1544, 1544, 1544, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1547, 1548, 1549, 1550, 1551, 1, 1, 1,
        1552, 1552, 1552, 1552, 1552, 1552, 1552, 1552, 1553, 1553, 1553, 1553, 1553, 1553, 1553, 1554, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1556, 1556, 1556, 1556, 1556, 1556, 1556, 1557,
        1558, 1558, 1558, 1558, 1558, 1558, 1558, 1559, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1562, 1563, 1, 1, 1, 1, 1, 1,
        1564, 1564, 1564, 1564, 1564, 1564, 1564, 1564, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1567, 1567, 1567, 1567, 1567, 1567, 1567, 1567,
        1568, 1568, 1568, 1568, 1568, 1568, 1568, 1568, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1571, 1572, 1, 1, 1, 1, 1, 1,
        1573, 1573, 1573, 1573, 1573, 1573, 1573, 1573, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1576, 1576, 1576, 1576, 1576, 1576, 1576, 1576,
        1577, 1577, 1577, 1577, 1577, 1577, 1577, 1577, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1580, 1581, 1, 1, 1, 1, 1, 1,
        1582, 1582, 1582, 1582, 1582, 1582, 1582, 1582, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1585, 1585, 1585, 1585, 1585, 1585, 1585, 1585,
        1586, 1586, 1586, 1586, 1586, 1586, 1586, 1586, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1589, 1590, 1591, 1592, 1593, 1, 1, 1,
        1503, 1503, 1503, 1503, 1503, 1503, 1503, 1503, 1, 1, 1, 1, 1, 1, 1, 1, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1505, 1505, 1505, 1505, 1505, 1505, 1505, 1506,
        1507, 1507, 1507, 1507, 1507, 1507, 1507, 1508, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1511, 1512, 1513, 1514, 1515, 1, 1, 1,
        1516, 1516, 1516, 1516, 1516, 1516, 1516, 1516, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1519, 1519, 1519, 1519, 1519, 1519, 1519, 1519,
        1520, 1520, 1520, 1520, 1520, 1520, 1520, 1520, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1523, 1524, 1525, 1526, 1527, 1, 1, 1,
        1528, 1528, 1528, 1528, 1528, 1528, 1528, 1528, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1531, 1531, 1531, 1531, 1531, 1531, 1531, 1531,
        1532, 1532, 1532, 1532, 1532, 1532, 1532, 1532, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1535, 1536, 1537, 1538, 1539, 1, 1, 1,
        1540, 1540, 1540, 1540, 1540, 1540, 1540, 1540, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1543, 1543, 1543, 1543, 1543, 1543, 1543, 1543,
        1544, 1544, 1544, 1544, 1544, 1544, 1544, 1544, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1547, 1548, 1549, 1550, 1551, 1, 1, 1,
        1552, 1552, 1552, 1552, 1552, 1552, 1552, 1552, 1553, 1553, 1553, 1553, 1553, 1553, 1553, 1554, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1556, 1556, 1556, 1556, 1556, 1556, 1556, 1557,
        1558, 1558, 1558, 1558, 1558, 1558, 1558, 1559, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1562, 1563, 1, 1, 1, 1, 1, 1,
        1564, 1564, 1564, 1564, 1564, 1564, 1564, 1564, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1567, 1567, 1567, 1567, 1567, 1567, 1567, 1567,
        1568, 1568, 1568, 1568, 1568, 1568, 1568, 1568, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1571, 1572, 1, 1, 1, 1, 1, 1,
        1573, 1573, 1573, 1573, 1573, 1573, 1573, 1573, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1576, 1576, 1576, 1576, 1576, 1576, 1576, 1576,
        1577, 1577, 1577, 1577, 1577, 1577, 1577, 1577, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1580, 1581, 1, 1, 1, 1, 1, 1,
        1582, 1582, 1582, 1582, 1582, 1582, 1582, 1582, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1585, 1585, 1585, 1585, 1585, 1585, 1585, 1585,
        1586, 1586, 1586, 1586, 1586, 1586, 1586, 1586, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1589, 1590, 1591, 1592, 1593, 1, 1, 1,
        1503, 1503, 1503, 1503, 1503, 1503, 1503, 1503, 1, 1, 1, 1, 1, 1, 1, 1, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1505, 1505, 1505, 1505, 1505, 1505, 1505, 1506,
        1507, 1507, 1507, 1507, 1507, 1507, 1507, 1508, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1511, 1512, 1513, 1514, 1515, 1, 1, 1,
        1516, 1516, 1516, 1516, 1516, 1516, 1516, 1516, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1519, 1519, 1519, 1519, 1519, 1519, 1519, 1519,
        1520, 1520, 1520, 1520, 1520, 1520, 1520, 1520, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1523, 1524, 1525, 1526, 1527, 1, 1, 1,
        1528, 1528, 1528, 1528, 1528, 1528, 1528, 1528, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1531, 1531, 1531, 1531, 1531, 1531, 1531, 1531,
        1532, 1532, 1532, 1532, 1532, 1532, 1532, 1532, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1535, 1536, 1537, 1538, 1539, 1, 1, 1,
        1540, 1540, 1540, 1540, 1540, 1540, 1540, 1540, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1543, 1543, 1543, 1543, 1543, 1543, 1543, 1543,
        1544, 1544, 1544, 1544, 1544, 1544, 1544, 1544, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1547, 1548, 1549, 1550, 1551, 1, 1, 1,
        1552, 1552, 1552, 1552, 1552, 1552, 1552, 1552, 1553, 1553, 1553, 1553, 1553, 1553, 1553, 1554, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1556, 1556, 1556, 1556, 1556, 1556, 1556, 1557,
        1558, 1558, 1558, 1558, 1558, 1558, 1558, 1559, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1562, 1563, 1, 1, 1, 1, 1, 1,
        1564, 1564, 1564, 1564, 1564, 1564, 1564, 1564, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1567, 1567, 1567, 1567, 1567, 1567, 1567, 1567,
        1568, 1568, 1568, 1568, 1568, 1568, 1568, 1568, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1571, 1572, 1, 1, 1, 1, 1, 1,
        1573, 1573, 1573, 1573, 1573, 1573, 1573, 1573, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1576, 1576, 1576, 1576, 1576, 1576, 1576, 1576,
        1577, 1577, 1577, 1577, 1577, 1577, 1577, 1577, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1580, 1581, 1, 1, 1, 1, 1, 1,
        1582, 1582, 1582, 1582, 1582, 1582, 1582, 1582, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1585, 1585, 1585, 1585, 1585, 1585, 1585, 1585,
        1586, 1586, 1586, 1586, 1586, 1586, 1586, 1586, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1589, 1590, 1591, 1592, 1593, 1, 1, 1,
        1503, 1503, 1503, 1503, 1503, 1503, 1503, 1503, 1, 1, 1, 1, 1, 1, 1, 1, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1505, 1505, 1505, 1505, 1505, 1505, 1505, 1506,
        1507, 1507, 1507, 1507, 1507, 1507, 1507, 1508, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1511, 1512, 1513, 1514, 1515, 1, 1, 1,
        1516, 1516, 1516, 1516, 1516, 1516, 1516, 1516, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1519, 1519, 1519, 1519, 1519, 1519, 1519, 1519,
        1520, 1520, 1520, 1520, 1520, 1520, 1520, 1520, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1523, 1524, 1525, 1526, 1527, 1, 1, 1,
        1528, 1528, 1528, 1528, 1528, 1528, 1528, 1528, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1531, 1531, 1531, 1531, 1531, 1531, 1531, 1531,
        1532, 1532, 1532, 1532, 1532, 1532, 1532, 1532, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1535, 1536, 1537, 1538, 1539, 1, 1, 1,
        1540, 1540, 1540, 1540, 1540, 1540, 1540, 1540, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1543, 1543, 1543, 1543, 1543, 1543, 1543, 1543,
        1544, 1544, 1544, 1544, 1544, 1544, 1544, 1544, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1547, 1548, 1549, 1550, 1551, 1, 1, 1,
        1552, 1552, 1552, 1552, 1552, 1552, 1552, 1552, 1553, 1553, 1553, 1553, 1553, 1553, 1553, 1554, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1556, 1556, 1556, 1556, 1556, 1556, 1556, 1557,
        1558, 1558, 1558, 1558, 1558, 1558, 1558, 1559, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1562, 1563, 1, 1, 1, 1, 1, 1,
        1564, 1564, 1564, 1564, 1564, 1564, 1564, 1564, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1567, 1567, 1567, 1567, 1567, 1567, 1567, 1567,
        1568, 1568, 1568, 1568, 1568, 1568, 1568, 1568, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1571, 1572, 1, 1, 1, 1, 1, 1,
        1573, 1573, 1573, 1573, 1573, 1573, 1573, 1573, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1576, 1576, 1576, 1576, 1576, 1576, 1576, 1576,
        1577, 1577, 1577, 1577, 1577, 1577, 1577, 1577, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1580, 1581, 1, 1, 1, 1, 1, 1,
        1582, 1582, 1582, 1582, 1582, 1582, 1582, 1582, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1585, 1585, 1585, 1585, 1585, 1585, 1585, 1585,
        1586, 1586, 1586, 1586, 1586, 1586, 1586, 1586, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1589, 1590, 1591, 1592, 1593, 1, 1, 1,
        1503, 1503, 1503, 1503, 1503, 1503, 1503, 1503, 1, 1, 1, 1, 1, 1, 1, 1, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1505, 1505, 1505, 1505, 1505, 1505, 1505, 1506,
        1507, 1507, 1507, 1507, 1507, 1507, 1507, 1508, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1511, 1512, 1513, 1514, 1515, 1, 1, 1,
        1516, 1516, 1516, 1516, 1516, 1516, 1516, 1516, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1519, 1519, 1519, 1519, 1519, 1519, 1519, 1519,
        1520, 1520, 1520, 1520, 1520, 1520, 1520, 1520, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1523, 1524, 1525, 1526, 1527, 1, 1, 1,
        1528, 1528, 1528, 1528, 1528, 1528, 1528, 1528, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1531, 1531, 1531, 1531, 1531, 1531, 1531, 1531,
        1532, 1532, 1532, 1532, 1532, 1532, 1532, 1532, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1535, 1536, 1537, 1538, 1539, 1, 1, 1,
        1540, 1540, 1540, 1540, 1540, 1540, 1540, 1540, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1543, 1543, 1543, 1543, 1543, 1543, 1543, 1543,
        1544, 1544, 1544, 1544, 1544, 1544, 1544, 1544, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1547, 1548, 1549, 1550, 1551, 1, 1, 1,
        1552, 1552, 1552, 1552, 1552, 1552, 1552, 1552, 1553, 1553, 1553, 1553, 1553, 1553, 1553, 1554, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1556, 1556, 1556, 1556, 1556, 1556, 1556, 1557,
        1558, 1558, 1558, 1558, 1558, 1558, 1558, 1559, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1562, 1563, 1, 1, 1, 1, 1, 1,
        1564, 1564, 1564, 1564, 1564, 1564, 1564, 1564, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1567, 1567, 1567, 1567, 1567, 1567, 1567, 1567,
        1568, 1568, 1568, 1568, 1568, 1568, 1568, 1568, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1571, 1572, 1, 1, 1, 1, 1, 1,
        1573, 1573, 1573, 1573, 1573, 1573, 1573, 1573, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1576, 1576, 1576, 1576, 1576, 1576, 1576, 1576,
        1577, 1577, 1577, 1577, 1577, 1577, 1577, 1577, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1580, 1581, 1, 1, 1, 1, 1, 1,
        1582, 1582, 1582, 1582, 1582, 1582, 1582, 1582, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1585, 1585, 1585, 1585, 1585, 1585, 1585, 1585,
        1586, 1586, 1586, 1586, 1586, 1586, 1586, 1586, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1589, 1590, 1591, 1592, 1593, 1, 1, 1,
        1503, 1503, 1503, 1503, 1503, 1503, 1503, 1503, 1, 1, 1, 1, 1, 1, 1, 1, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1505, 1505, 1505, 1505, 1505, 1505, 1505, 1506,
        1507, 1507, 1507, 1507, 1507, 1507, 1507, 1508, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1511, 1512, 1513, 1514, 1515, 1, 1, 1,
        1516, 1516, 1516, 1516, 1516, 1516, 1516, 1516, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1519, 1519, 1519, 1519, 1519, 1519, 1519, 1519,
        1520, 1520, 1520, 1520, 1520, 1520, 1520, 1520, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1523, 1524, 1525, 1526, 1527, 1, 1, 1,
        1528, 1528, 1528, 1528, 1528, 1528, 1528, 1528, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1531, 1531, 1531, 1531, 1531, 1531, 1531, 1531,
        1532, 1532, 1532, 1532, 1532, 1532, 1532, 1532, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1535, 1536, 1537, 1538, 1539, 1, 1, 1,
        1540, 1540, 1540, 1540, 1540, 1540, 1540, 1540, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1543, 1543, 1543, 1543, 1543, 1543, 1543, 1543,
        1544, 1544, 1544, 1544, 1544, 1544, 1544, 1544, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1547, 1548, 1549, 1550, 1551, 1, 1, 1,
        1552, 1552, 1552, 1552, 1552, 1552, 1552, 1552, 1553, 1553, 1553, 1553, 1553, 1553, 1553, 1554, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1556, 1556, 1556, 1556, 1556, 1556, 1556, 1557,
        1558, 1558, 1558, 1558, 1558, 1558, 1558, 1559, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1562, 1563, 1, 1, 1, 1, 1, 1,
        1564, 1564, 1564, 1564, 1564, 1564, 1564, 1564, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1567, 1567, 1567, 1567, 1567, 1567, 1567, 1567,
        1568, 1568, 1568, 1568, 1568, 1568, 1568, 1568, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1571, 1572, 1, 1, 1, 1, 1, 1,
        1573, 1573, 1573, 1573, 1573, 1573, 1573, 1573, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1576, 1576, 1576, 1576, 1576, 1576, 1576, 1576,
        1577, 1577, 1577, 1577, 1577, 1577, 1577, 1577, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1580, 1581, 1, 1, 1, 1, 1, 1,
        1582, 1582, 1582, 1582, 1582, 1582, 1582, 1582, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1585, 1585, 1585, 1585, 1585, 1585, 1585, 1585,
        1586, 1586, 1586, 1586, 1586, 1586, 1586, 1586, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1589, 1590, 1591, 1592, 1593, 1, 1, 1,
        1503, 1503, 1503, 1503, 1503, 1503, 1503, 1503, 1, 1, 1, 1, 1, 1, 1, 1, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1505, 1505, 1505, 1505, 1505, 1505, 1505, 1506,
        1507, 1507, 1507, 1507, 1507, 1507, 1507, 1508, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1511, 1512, 1513, 1514, 1515, 1, 1, 1,
        1516, 1516, 1516, 1516, 1516, 1516, 1516, 1516, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1519, 1519, 1519, 1519, 1519, 1519, 1519, 1519,
        1520, 1520, 1520, 1520, 1520, 1520, 1520, 1520, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1523, 1524, 1525, 1526, 1527, 1, 1, 1,
        1528, 1528, 1528, 1528, 1528, 1528, 1528, 1528, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1531, 1531, 1531, 1531, 1531, 1531, 1531, 1531,
        1532, 1532, 1532, 1532, 1532, 1532, 1532, 1532, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1535, 1536, 1537, 1538, 1539, 1, 1, 1,
        1540, 1540, 1540, 1540, 1540, 1540, 1540, 1540, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1543, 1543, 1543, 1543, 1543, 1543, 1543, 1543,
        1544, 1544, 1544, 1544, 1544, 1544, 1544, 1544, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1547, 1548, 1549, 1550, 1551, 1, 1, 1,
        1552, 1552, 1552, 1552, 1552, 1552, 1552, 1552, 1553, 1553, 1553, 1553, 1553, 1553, 1553, 1554, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1556, 1556, 1556, 1556, 1556, 1556, 1556, 1557,
        1558, 1558, 1558, 1558, 1558, 1558, 1558, 1559, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1562, 1563, 1, 1, 1, 1, 1, 1,
        1564, 1564, 1564, 1564, 1564, 1564, 1564, 1564, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1567, 1567, 1567, 1567, 1567, 1567, 1567, 1567,
        1568, 1568, 1568, 1568, 1568, 1568, 1568, 1568, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1571, 1572, 1, 1, 1, 1, 1, 1,
        1573, 1573, 1573, 1573, 1573, 1573, 1573, 1573, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1576, 1576, 1576, 1576, 1576, 1576, 1576, 1576,
        1577, 1577, 1577, 1577, 1577, 1577, 1577, 1577, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1580, 1581, 1, 1, 1, 1, 1, 1,
        1582, 1582, 1582, 1582, 1582, 1582, 1582, 1582, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1585, 1585, 1585, 1585, 1585, 1585, 1585, 1585,
        1586, 1586, 1586, 1586, 1586, 1586, 1586, 1586, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1589, 1590, 1591, 1592, 1593, 1, 1, 1,
        1503, 1503, 1503, 1503, 1503, 1503, 1503, 1503, 1, 1, 1, 1, 1, 1, 1, 1, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1504, 1505, 1505, 1505, 1505, 1505, 1505, 1505, 1506,
        1507, 1507, 1507, 1507, 1507, 1507, 1507, 1508, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1509, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1510, 1511, 1512, 1513, 1514, 1515, 1, 1, 1,
        1516, 1516, 1516, 1516, 1516, 1516, 1516, 1516, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1517, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1518, 1519, 1519, 1519, 1519, 1519, 1519, 1519, 1519,
        1520, 1520, 1520, 1520, 1520, 1520, 1520, 1520, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1521, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1522, 1523, 1524, 1525, 1526, 1527, 1, 1, 1,
        1528, 1528, 1528, 1528, 1528, 1528, 1528, 1528, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1529, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1530, 1531, 1531, 1531, 1531, 1531, 1531, 1531, 1531,
        1532, 1532, 1532, 1532, 1532, 1532, 1532, 1532, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1533, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1534, 1535, 1536, 1537, 1538, 1539, 1, 1, 1,
        1540, 1540, 1540, 1540, 1540, 1540, 1540, 1540, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1541, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1542, 1543, 1543, 1543, 1543, 1543, 1543, 1543, 1543,
        1544, 1544, 1544, 1544, 1544, 1544, 1544, 1544, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1545, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1546, 1547, 1548, 1549, 1550, 1551, 1, 1, 1,
        1552, 1552, 1552, 1552, 1552, 1552, 1552, 1552, 1594, 1594, 1594, 1594, 1594, 1594, 1594, 1595, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1555, 1556, 1556, 1556, 1556, 1556, 1556, 1556, 1557,
        1558, 1558, 1558, 1558, 1558, 1558, 1558, 1559, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1560, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1561, 1562, 1563, 1, 1, 1, 1, 1, 1,
        1564, 1564, 1564, 1564, 1564, 1564, 1564, 1564, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1565, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1566, 1567, 1567, 1567, 1567, 1567, 1567, 1567, 1567,
        1568, 1568, 1568, 1568, 1568, 1568, 1568, 1568, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1569, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1570, 1571, 1572, 1, 1, 1, 1, 1, 1,
        1573, 1573, 1573, 1573, 1573, 1573, 1573, 1573, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1574, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1575, 1576, 1576, 1576, 1576, 1576, 1576, 1576, 1576,
        1577, 1577, 1577, 1577, 1577, 1577, 1577, 1577, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1578, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1579, 1580, 1581, 1, 1, 1, 1, 1, 1,
        1582, 1582, 1582, 1582, 1582, 1582, 1582, 1582, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1583, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1584, 1585, 1585, 1585, 1585, 1585, 1585, 1585, 1585,
        1586, 1586, 1586, 1586, 1586, 1586, 1586, 1586, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1587, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1588, 1589, 1590, 1591, 1592, 1593, 1, 1, 1,
        1596, 1596, 1596, 1596, 1596, 1596, 1596, 1596, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1599, 1599, 1599, 1599, 1599, 1599, 1599, 1599,
        1600, 1600, 1600, 1600, 1600, 1600, 1600, 1600, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1603, 1603, 1603, 1603, 1603, 1603, 1603, 1603,
        1604, 1604, 1604, 1604, 1604, 1604, 1604, 1604, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1607, 1607, 1607, 1607, 1607, 1607, 1607, 1607,
        1608, 1608, 1608, 1608, 1608, 1608, 1608, 1608, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1611, 1611, 1611, 1611, 1611, 1611, 1611, 1611,
        1612, 1612, 1612, 1612, 1612, 1612, 1612, 1612, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1615, 1615, 1615, 1615, 1615, 1615, 1615, 1615,
        1616, 1616, 1616, 1616, 1616, 1616, 1616, 1616, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1619, 1619, 1619, 1619, 1619, 1619, 1619, 1619,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1620, 1620, 1620, 1620, 1620, 1620, 1620, 1620, 1621, 1621, 1621, 1621, 1621, 1621, 1621, 1621,
        1622, 1622, 1622, 1622, 1622, 1622, 1622, 1622, 1623, 1623, 1623, 1623, 1623, 1623, 1623, 1623, 1624, 1624, 1624, 1624, 1624, 1624, 1624, 1624, 1625, 1626, 1, 1, 1, 1, 1, 1,
        1627, 1627, 1627, 1627, 1627, 1627, 1627, 1627, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1630, 1630, 1630, 1630, 1630, 1630, 1630, 1630,
        1631, 1631, 1631, 1631, 1631, 1631, 1631, 1631, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1634, 1634, 1634, 1634, 1634, 1634, 1634, 1634,
        1635, 1635, 1635, 1635, 1635, 1635, 1635, 1635, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1638, 1638, 1638, 1638, 1638, 1638, 1638, 1638,
        1639, 1639, 1639, 1639, 1639, 1639, 1639, 1639, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1642, 1642, 1642, 1642, 1642, 1642, 1642, 1642,
        1643, 1643, 1643, 1643, 1643, 1643, 1643, 1643, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1646, 1646, 1646, 1646, 1646, 1646, 1646, 1646,
        1647, 1647, 1647, 1647, 1647, 1647, 1647, 1647, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1650, 1650, 1650, 1650, 1650, 1650, 1650, 1650,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1651, 1651, 1651, 1651, 1651, 1651, 1651, 1651, 1652, 1652, 1652, 1652, 1652, 1652, 1652, 1652,
        1653, 1653, 1653, 1653, 1653, 1653, 1653, 1653, 1654, 1654, 1654, 1654, 1654, 1654, 1654, 1654, 1655, 1655, 1655, 1655, 1655, 1655, 1655, 1655, 1656, 1657, 1, 1, 1, 1, 1, 1,
        1596, 1596, 1596, 1596, 1596, 1596, 1596, 1596, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1599, 1599, 1599, 1599, 1599, 1599, 1599, 1599,
        1600, 1600, 1600, 1600, 1600, 1600, 1600, 1600, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1603, 1603, 1603, 1603, 1603, 1603, 1603, 1603,
        1604, 1604, 1604, 1604, 1604, 1604, 1604, 1604, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1607, 1607, 1607, 1607, 1607, 1607, 1607, 1607,
        1608, 1608, 1608, 1608, 1608, 1608, 1608, 1608, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1611, 1611, 1611, 1611, 1611, 1611, 1611, 1611,
        1612, 1612, 1612, 1612, 1612, 1612, 1612, 1612, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1615, 1615, 1615, 1615, 1615, 1615, 1615, 1615,
        1616, 1616, 1616, 1616, 1616, 1616, 1616, 1616, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1619, 1619, 1619, 1619, 1619, 1619, 1619, 1619,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1658, 1658, 1658, 1658, 1658, 1658, 1658, 1658, 1659, 1659, 1659, 1659, 1659, 1659, 1659, 1659,
        1660, 1660, 1660, 1660, 1660, 1660, 1660, 1660, 1661, 1661, 1661, 1661, 1661, 1661, 1661, 1661, 1662, 1662, 1662, 1662, 1662, 1662, 1662, 1662, 1663, 1664, 1, 1, 1, 1, 1, 1,
        1627, 1627, 1627, 1627, 1627, 1627, 1627, 1627, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1630, 1630, 1630, 1630, 1630, 1630, 1630, 1630,
        1631, 1631, 1631, 1631, 1631, 1631, 1631, 1631, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1634, 1634, 1634, 1634, 1634, 1634, 1634, 1634,
        1635, 1635, 1635, 1635, 1635, 1635, 1635, 1635, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1638, 1638, 1638, 1638, 1638, 1638, 1638, 1638,
        1639, 1639, 1639, 1639, 1639, 1639, 1639, 1639, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1642, 1642, 1642, 1642, 1642, 1642, 1642, 1642,
        1643, 1643, 1643, 1643, 1643, 1643, 1643, 1643, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1646, 1646, 1646, 1646, 1646, 1646, 1646, 1646,
        1647, 1647, 1647, 1647, 1647, 1647, 1647, 1647, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1650, 1650, 1650, 1650, 1650, 1650, 1650, 1650,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1665, 1665, 1665, 1665, 1665, 1665, 1665, 1665, 1666, 1666, 1666, 1666, 1666, 1666, 1666, 1666,
        1667, 1667, 1667, 1667, 1667, 1667, 1667, 1667, 1668, 1668, 1668, 1668, 1668, 1668, 1668, 1668, 1669, 1669, 1669, 1669, 1669, 1669, 1669, 1669, 1670, 1671, 1, 1, 1, 1, 1, 1,
        1596, 1596, 1596, 1596, 1596, 1596, 1596, 1596, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1599, 1599, 1599, 1599, 1599, 1599, 1599, 1599,
        1600, 1600, 1600, 1600, 1600, 1600, 1600, 1600, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1603, 1603, 1603, 1603, 1603, 1603, 1603, 1603,
        1604, 1604, 1604, 1604, 1604, 1604, 1604, 1604, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1607, 1607, 1607, 1607, 1607, 1607, 1607, 1607,
        1608, 1608, 1608, 1608, 1608, 1608, 1608, 1608, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1611, 1611, 1611, 1611, 1611, 1611, 1611, 1611,
        1612, 1612, 1612, 1612, 1612, 1612, 1612, 1612, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1615, 1615, 1615, 1615, 1615, 1615, 1615, 1615,
        1616, 1616, 1616, 1616, 1616, 1616, 1616, 1616, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1619, 1619, 1619, 1619, 1619, 1619, 1619, 1619,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1672, 1672, 1672, 1672, 1672, 1672, 1672, 1672, 1673, 1673, 1673, 1673, 1673, 1673, 1673, 1673,
        1674, 1674, 1674, 1674, 1674, 1674, 1674, 1674, 1675, 1675, 1675, 1675, 1675, 1675, 1675, 1675, 1676, 1676, 1676, 1676, 1676, 1676, 1676, 1676, 1677, 1678, 1, 1, 1, 1, 1, 1,
        1627, 1627, 1627, 1627, 1627, 1627, 1627, 1627, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1630, 1630, 1630, 1630, 1630, 1630, 1630, 1630,
        1631, 1631, 1631, 1631, 1631, 1631, 1631, 1631, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1634, 1634, 1634, 1634, 1634, 1634, 1634, 1634,
        1635, 1635, 1635, 1635, 1635, 1635, 1635, 1635, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1638, 1638, 1638, 1638, 1638, 1638, 1638, 1638,
        1639, 1639, 1639, 1639, 1639, 1639, 1639, 1639, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1642, 1642, 1642, 1642, 1642, 1642, 1642, 1642,
        1643, 1643, 1643, 1643, 1643, 1643, 1643, 1643, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1646, 1646, 1646, 1646, 1646, 1646, 1646, 1646,
        1647, 1647, 1647, 1647, 1647, 1647, 1647, 1647, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1650, 1650, 1650, 1650, 1650, 1650, 1650, 1650,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1679, 1679, 1679, 1679, 1679, 1679, 1679, 1679, 1680, 1680, 1680, 1680, 1680, 1680, 1680, 1680,
        1681, 1681, 1681, 1681, 1681, 1681, 1681, 1681, 1682, 1682, 1682, 1682, 1682, 1682, 1682, 1682, 1683, 1683, 1683, 1683, 1683, 1683, 1683, 1683, 1684, 1685, 1, 1, 1, 1, 1, 1,
        1596, 1596, 1596, 1596, 1596, 1596, 1596, 1596, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1599, 1599, 1599, 1599, 1599, 1599, 1599, 1599,
        1600, 1600, 1600, 1600, 1600, 1600, 1600, 1600, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1603, 1603, 1603, 1603, 1603, 1603, 1603, 1603,
        1604, 1604, 1604, 1604, 1604, 1604, 1604, 1604, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1607, 1607, 1607, 1607, 1607, 1607, 1607, 1607,
        1608, 1608, 1608, 1608, 1608, 1608, 1608, 1608, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1611, 1611, 1611, 1611, 1611, 1611, 1611, 1611,
        1612, 1612, 1612, 1612, 1612, 1612, 1612, 1612, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1615, 1615, 1615, 1615, 1615, 1615, 1615, 1615,
        1616, 1616, 1616, 1616, 1616, 1616, 1616, 1616, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1619, 1619, 1619, 1619, 1619, 1619, 1619, 1619,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1686, 1686, 1686, 1686, 1686, 1686, 1686, 1686, 1687, 1687, 1687, 1687, 1687, 1687, 1687, 1687,
        1688, 1688, 1688, 1688, 1688, 1688, 1688, 1688, 1689, 1689, 1689, 1689, 1689, 1689, 1689, 1689, 1690, 1690, 1690, 1690, 1690, 1690, 1690, 1690, 1691, 1692, 1, 1, 1, 1, 1, 1,
        1627, 1627, 1627, 1627, 1627, 1627, 1627, 1627, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1630, 1630, 1630, 1630, 1630, 1630, 1630, 1630,
        1631, 1631, 1631, 1631, 1631, 1631, 1631, 1631, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1634, 1634, 1634, 1634, 1634, 1634, 1634, 1634,
        1635, 1635, 1635, 1635, 1635, 1635, 1635, 1635, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1638, 1638, 1638, 1638, 1638, 1638, 1638, 1638,
        1639, 1639, 1639, 1639, 1639, 1639, 1639, 1639, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1642, 1642, 1642, 1642, 1642, 1642, 1642, 1642,
        1643, 1643, 1643, 1643, 1643, 1643, 1643, 1643, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1646, 1646, 1646, 1646, 1646, 1646, 1646, 1646,
        1647, 1647, 1647, 1647, 1647, 1647, 1647, 1647, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1650, 1650, 1650, 1650, 1650, 1650, 1650, 1650,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1693, 1693, 1693, 1693, 1693, 1693, 1693, 1693, 1694, 1694, 1694, 1694, 1694, 1694, 1694, 1694,
        1695, 1695, 1695, 1695, 1695, 1695, 1695, 1695, 1696, 1696, 1696, 1696, 1696, 1696, 1696, 1696, 1697, 1697, 1697, 1697, 1697, 1697, 1697, 1697, 1698, 1699, 1, 1, 1, 1, 1, 1,
        1596, 1596, 1596, 1596, 1596, 1596, 1596, 1596, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1599, 1599, 1599, 1599, 1599, 1599, 1599, 1599,
        1600, 1600, 1600, 1600, 1600, 1600, 1600, 1600, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1603, 1603, 1603, 1603, 1603, 1603, 1603, 1603,
        1604, 1604, 1604, 1604, 1604, 1604, 1604, 1604, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1607, 1607, 1607, 1607, 1607, 1607, 1607, 1607,
        1608, 1608, 1608, 1608, 1608, 1608, 1608, 1608, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1611, 1611, 1611, 1611, 1611, 1611, 1611, 1611,
        1612, 1612, 1612, 1612, 1612, 1612, 1612, 1612, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1615, 1615, 1615, 1615, 1615, 1615, 1615, 1615,
        1616, 1616, 1616, 1616, 1616, 1616, 1616, 1616, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1619, 1619, 1619, 1619, 1619, 1619, 1619, 1619,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1627, 1627, 1627, 1627, 1627, 1627, 1627, 1627, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1630, 1630, 1630, 1630, 1630, 1630, 1630, 1630,
        1631, 1631, 1631, 1631, 1631, 1631, 1631, 1631, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1634, 1634, 1634, 1634, 1634, 1634, 1634, 1634,
        1635, 1635, 1635, 1635, 1635, 1635, 1635, 1635, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1638, 1638, 1638, 1638, 1638, 1638, 1638, 1638,
        1639, 1639, 1639, 1639, 1639, 1639, 1639, 1639, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1642, 1642, 1642, 1642, 1642, 1642, 1642, 1642,
        1643, 1643, 1643, 1643, 1643, 1643, 1643, 1643, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1646, 1646, 1646, 1646, 1646, 1646, 1646, 1646,
        1647, 1647, 1647, 1647, 1647, 1647, 1647, 1647, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1650, 1650, 1650, 1650, 1650, 1650, 1650, 1650,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1596, 1596, 1596, 1596, 1596, 1596, 1596, 1596, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1599, 1599, 1599, 1599, 1599, 1599, 1599, 1599,
        1600, 1600, 1600, 1600, 1600, 1600, 1600, 1600, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1603, 1603, 1603, 1603, 1603, 1603, 1603, 1603,
        1604, 1604, 1604, 1604, 1604, 1604, 1604, 1604, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1607, 1607, 1607, 1607, 1607, 1607, 1607, 1607,
        1608, 1608, 1608, 1608, 1608, 1608, 1608, 1608, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1611, 1611, 1611, 1611, 1611, 1611, 1611, 1611,
        1612, 1612, 1612, 1612, 1612, 1612, 1612, 1612, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1615, 1615, 1615, 1615, 1615, 1615, 1615, 1615,
        1616, 1616, 1616, 1616, 1616, 1616, 1616, 1616, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1619, 1619, 1619, 1619, 1619, 1619, 1619, 1619,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1627, 1627, 1627, 1627, 1627, 1627, 1627, 1627, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1630, 1630, 1630, 1630, 1630, 1630, 1630, 1630,
        1631, 1631, 1631, 1631, 1631, 1631, 1631, 1631, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1634, 1634, 1634, 1634, 1634, 1634, 1634, 1634,
        1635, 1635, 1635, 1635, 1635, 1635, 1635, 1635, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1638, 1638, 1638, 1638, 1638, 1638, 1638, 1638,
        1639, 1639, 1639, 1639, 1639, 1639, 1639, 1639, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1642, 1642, 1642, 1642, 1642, 1642, 1642, 1642,
        1643, 1643, 1643, 1643, 1643, 1643, 1643, 1643, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1646, 1646, 1646, 1646, 1646, 1646, 1646, 1646,
        1647, 1647, 1647, 1647, 1647, 1647, 1647, 1647, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1650, 1650, 1650, 1650, 1650, 1650, 1650, 1650,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1596, 1596, 1596, 1596, 1596, 1596, 1596, 1596, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1599, 1599, 1599, 1599, 1599, 1599, 1599, 1599,
        1600, 1600, 1600, 1600, 1600, 1600, 1600, 1600, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1603, 1603, 1603, 1603, 1603, 1603, 1603, 1603,
        1604, 1604, 1604, 1604, 1604, 1604, 1604, 1604, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1607, 1607, 1607, 1607, 1607, 1607, 1607, 1607,
        1608, 1608, 1608, 1608, 1608, 1608, 1608, 1608, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1611, 1611, 1611, 1611, 1611, 1611, 1611, 1611,
        1612, 1612, 1612, 1612, 1612, 1612, 1612, 1612, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1615, 1615, 1615, 1615, 1615, 1615, 1615, 1615,
        1616, 1616, 1616, 1616, 1616, 1616, 1616, 1616, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1619, 1619, 1619, 1619, 1619, 1619, 1619, 1619,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1627, 1627, 1627, 1627, 1627, 1627, 1627, 1627, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1630, 1630, 1630, 1630, 1630, 1630, 1630, 1630,
        1631, 1631, 1631, 1631, 1631, 1631, 1631, 1631, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1634, 1634, 1634, 1634, 1634, 1634, 1634, 1634,
        1635, 1635, 1635, 1635, 1635, 1635, 1635, 1635, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1638, 1638, 1638, 1638, 1638, 1638, 1638, 1638,
        1639, 1639, 1639, 1639, 1639, 1639, 1639, 1639, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1642, 1642, 1642, 1642, 1642, 1642, 1642, 1642,
        1643, 1643, 1643, 1643, 1643, 1643, 1643, 1643, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1646, 1646, 1646, 1646, 1646, 1646, 1646, 1646,
        1647, 1647, 1647, 1647, 1647, 1647, 1647, 1647, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1650, 1650, 1650, 1650, 1650, 1650, 1650, 1650,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1596, 1596, 1596, 1596, 1596, 1596, 1596, 1596, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1597, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1598, 1599, 1599, 1599, 1599, 1599, 1599, 1599, 1599,
        1600, 1600, 1600, 1600, 1600, 1600, 1600, 1600, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1601, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1602, 1603, 1603, 1603, 1603, 1603, 1603, 1603, 1603,
        1604, 1604, 1604, 1604, 1604, 1604, 1604, 1604, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1605, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1606, 1607, 1607, 1607, 1607, 1607, 1607, 1607, 1607,
        1608, 1608, 1608, 1608, 1608, 1608, 1608, 1608, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1609, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1610, 1611, 1611, 1611, 1611, 1611, 1611, 1611, 1611,
        1612, 1612, 1612, 1612, 1612, 1612, 1612, 1612, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1613, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1614, 1615, 1615, 1615, 1615, 1615, 1615, 1615, 1615,
        1616, 1616, 1616, 1616, 1616, 1616, 1616, 1616, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1617, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1618, 1619, 1619, 1619, 1619, 1619, 1619, 1619, 1619,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1627, 1627, 1627, 1627, 1627, 1627, 1627, 1627, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1628, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1629, 1630, 1630, 1630, 1630, 1630, 1630, 1630, 1630,
        1631, 1631, 1631, 1631, 1631, 1631, 1631, 1631, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1632, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1633, 1634, 1634, 1634, 1634, 1634, 1634, 1634, 1634,
        1635, 1635, 1635, 1635, 1635, 1635, 1635, 1635, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1636, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1637, 1638, 1638, 1638, 1638, 1638, 1638, 1638, 1638,
        1639, 1639, 1639, 1639, 1639, 1639, 1639, 1639, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1640, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1641, 1642, 1642, 1642, 1642, 1642, 1642, 1642, 1642,
        1643, 1643, 1643, 1643, 1643, 1643, 1643, 1643, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1644, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1645, 1646, 1646, 1646, 1646, 1646, 1646, 1646, 1646,
        1647, 1647, 1647, 1647, 1647, 1647, 1647, 1647, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1648, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1649, 1650, 1650, 1650, 1650, 1650, 1650, 1650, 1650,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
        1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700, 1700,
    ];
    private static ReadOnlySpan<byte> MainCycles =>
    [
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 0, 0, 140, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 0, 0, 140, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 0, 0, 0, 0, 0, 0, 0, 0, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196,
        210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 238, 238, 238, 238, 238, 238, 238, 238, 224, 252, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 112, 112, 112, 112, 112, 112, 112, 112, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 70, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 112, 112, 112, 112, 112, 112, 112, 112, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 0, 0, 140, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 0, 0, 140, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 0, 0, 0, 0, 0, 0, 0, 0, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196,
        210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 238, 238, 238, 238, 238, 238, 238, 238, 224, 252, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 112, 112, 112, 112, 112, 112, 112, 112, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 70, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 112, 112, 112, 112, 112, 112, 112, 112, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 0, 0, 0, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 0, 0, 0, 0, 0, 0, 0, 0, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196,
        210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 238, 238, 238, 238, 238, 238, 238, 238, 224, 252, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 112, 112, 112, 112, 112, 112, 112, 112, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 70, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 112, 112, 112, 112, 112, 112, 112, 112, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 0, 0, 0, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 0, 0, 0, 0, 0, 0, 0, 0, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196,
        210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 238, 238, 238, 238, 238, 238, 238, 238, 224, 252, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 112, 112, 112, 112, 112, 112, 112, 112, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 70, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 112, 112, 112, 112, 112, 112, 112, 112, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        70, 70, 70, 70, 70, 70, 70, 70, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 0, 0, 0, 0,
        70, 70, 70, 70, 70, 70, 70, 70, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 0, 0, 0, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 0, 0, 0, 0, 0, 0,
        70, 70, 70, 70, 70, 70, 70, 70, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 112, 112, 112, 112, 112, 112, 112, 112, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 70, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 112, 112, 112, 112, 112, 112, 112, 112, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 0, 0, 140, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 0, 0, 140, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 0, 0, 0, 0, 0, 0, 0, 0, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196,
        210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 238, 238, 238, 238, 238, 238, 238, 238, 224, 252, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 112, 112, 112, 112, 112, 112, 112, 112, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 70, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 112, 112, 112, 112, 112, 112, 112, 112, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 112, 112, 112, 112, 112, 112, 112, 112, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 70, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 112, 112, 112, 112, 112, 112, 112, 112, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 112, 112, 112, 112, 112, 112, 112, 112, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 70, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 112, 112, 112, 112, 112, 112, 112, 112, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 168, 168, 168, 168, 168, 168, 168, 168, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 0, 0, 0, 0, 0, 0, 0, 0, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 0, 0, 0, 0, 0, 0, 0, 0, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 0, 0, 0, 0, 0, 0, 0, 0, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 0, 0, 0, 0, 0, 0, 0, 0, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 0, 0, 0, 0, 0, 0, 0, 0, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 0, 0, 0, 0, 0, 0, 0, 0, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 0, 0, 0, 0, 0, 0, 0, 0, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 0, 0, 0, 0, 0, 0, 0, 0, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168,
        182, 182, 182, 182, 182, 182, 182, 182, 196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 196, 224, 196, 210, 168, 0, 0, 0,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182,
        196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 210, 238, 210, 224, 182, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168,
        182, 182, 182, 182, 182, 182, 182, 182, 196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 196, 224, 196, 210, 168, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168,
        182, 182, 182, 182, 182, 182, 182, 182, 196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 196, 224, 196, 210, 168, 0, 0, 0,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182,
        196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 210, 238, 210, 224, 182, 0, 0, 0,
        140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196,
        210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 238, 238, 238, 238, 238, 238, 238, 238, 224, 252, 224, 238, 196, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168,
        182, 182, 182, 182, 182, 182, 182, 182, 196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 196, 224, 196, 210, 168, 0, 0, 0,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182,
        196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 210, 238, 210, 224, 182, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168,
        182, 182, 182, 182, 182, 182, 182, 182, 196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 196, 224, 196, 210, 168, 0, 0, 0,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182,
        196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 210, 238, 210, 224, 182, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168,
        182, 182, 182, 182, 182, 182, 182, 182, 196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 196, 224, 196, 210, 168, 0, 0, 0,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182,
        196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 210, 238, 210, 224, 182, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168,
        182, 182, 182, 182, 182, 182, 182, 182, 196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 196, 224, 196, 210, 168, 0, 0, 0,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182,
        196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 210, 238, 210, 224, 182, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168,
        182, 182, 182, 182, 182, 182, 182, 182, 196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 196, 224, 196, 210, 168, 0, 0, 0,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182,
        196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 210, 238, 210, 224, 182, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168, 168,
        182, 182, 182, 182, 182, 182, 182, 182, 196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 196, 224, 196, 210, 168, 0, 0, 0,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182, 182,
        196, 196, 196, 196, 196, 196, 196, 196, 210, 210, 210, 210, 210, 210, 210, 210, 224, 224, 224, 224, 224, 224, 224, 224, 210, 238, 210, 224, 182, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 168, 182, 140, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 154, 182, 154, 168, 126, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 56, 84, 56, 84, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 56, 84, 56, 84, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 56, 84, 56, 84, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        84, 84, 84, 84, 84, 84, 84, 84, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 154, 154, 154, 154, 154, 154, 154, 154, 140, 168, 140, 154, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 56, 84, 56, 84, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 140, 140, 140, 140, 140, 140, 140, 140, 112, 140, 112, 140, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 56, 84, 56, 84, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 0, 0, 28, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 56, 84, 56, 84, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 56, 84, 56, 84, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 84, 84, 84, 84, 84, 84, 84, 84,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 0, 28, 28, 140, 0, 112, 0, 140, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 126, 126, 126, 126, 126, 126, 126, 126, 154, 154, 154, 154, 154, 154, 154, 154, 126, 140, 126, 154, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 70, 70, 70, 70, 70, 70, 70, 70, 98, 98, 98, 98, 98, 98, 98, 98, 70, 84, 70, 98, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 56, 84, 56, 84, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 98, 126, 98, 112, 70, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 98, 126, 98, 112, 70, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 98, 126, 98, 112, 70, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 98, 126, 98, 112, 70, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 98, 126, 98, 112, 70, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 98, 126, 98, 112, 70, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 98, 126, 98, 112, 70, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
        84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 98, 126, 98, 112, 70, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 98, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 42, 42, 42, 42, 42, 42, 42, 42, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 42, 42, 42, 42, 42, 42, 42, 42, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 42, 42, 42, 42, 42, 42, 42, 42, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 42, 42, 42, 42, 42, 42, 42, 42, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 42, 42, 42, 42, 42, 42, 42, 42, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 42, 42, 42, 42, 42, 42, 42, 42, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 42, 42, 42, 42, 42, 42, 42, 42, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 42, 42, 42, 42, 42, 42, 42, 42, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        42, 42, 42, 42, 42, 42, 42, 42, 56, 56, 56, 56, 56, 56, 56, 56, 70, 70, 70, 70, 70, 70, 70, 70, 56, 84, 56, 70, 28, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        70, 70, 70, 70, 70, 70, 70, 70, 84, 84, 84, 84, 84, 84, 84, 84, 98, 98, 98, 98, 98, 98, 98, 98, 84, 112, 84, 98, 56, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 112, 126, 84, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 126, 126, 126, 126, 126, 126, 126, 126, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 210, 210, 210, 210, 210, 210, 210, 210, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140, 140,
        154, 154, 154, 154, 154, 154, 154, 154, 168, 168, 168, 168, 168, 168, 168, 168, 182, 182, 182, 182, 182, 182, 182, 182, 168, 196, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
        112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 140, 140, 140, 140, 140, 140, 140, 140, 126, 154, 126, 140, 112, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84, 84,
        98, 98, 98, 98, 98, 98, 98, 98, 112, 112, 112, 112, 112, 112, 112, 112, 126, 126, 126, 126, 126, 126, 126, 126, 112, 140, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
    ];
    private static ReadOnlySpan<byte> SubCycles =>
    [
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 0, 0, 80, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 0, 0, 80, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 136, 136, 136, 136, 136, 136, 136, 136, 128, 144, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 64, 64, 64, 64, 64, 64, 64, 64, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 40, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 64, 64, 64, 64, 64, 64, 64, 64, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 0, 0, 80, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 0, 0, 80, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 136, 136, 136, 136, 136, 136, 136, 136, 128, 144, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 64, 64, 64, 64, 64, 64, 64, 64, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 40, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 64, 64, 64, 64, 64, 64, 64, 64, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 0, 0, 0, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 136, 136, 136, 136, 136, 136, 136, 136, 128, 144, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 64, 64, 64, 64, 64, 64, 64, 64, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 40, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 64, 64, 64, 64, 64, 64, 64, 64, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 0, 0, 0, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 136, 136, 136, 136, 136, 136, 136, 136, 128, 144, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 64, 64, 64, 64, 64, 64, 64, 64, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 40, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 64, 64, 64, 64, 64, 64, 64, 64, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        40, 40, 40, 40, 40, 40, 40, 40, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 0, 0, 0, 0,
        40, 40, 40, 40, 40, 40, 40, 40, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 0, 0, 0, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 0, 0, 0, 0, 0, 0,
        40, 40, 40, 40, 40, 40, 40, 40, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 64, 64, 64, 64, 64, 64, 64, 64, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 40, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 64, 64, 64, 64, 64, 64, 64, 64, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 0, 0, 80, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 0, 0, 80, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 0, 0, 0, 0, 0, 0, 0, 0, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 136, 136, 136, 136, 136, 136, 136, 136, 128, 144, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 64, 64, 64, 64, 64, 64, 64, 64, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 40, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 64, 64, 64, 64, 64, 64, 64, 64, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 64, 64, 64, 64, 64, 64, 64, 64, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 40, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 64, 64, 64, 64, 64, 64, 64, 64, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 64, 64, 64, 64, 64, 64, 64, 64, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 40, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 64, 64, 64, 64, 64, 64, 64, 64, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 96, 96, 96, 96, 96, 96, 96, 96, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 0, 0, 0, 0, 0, 0, 0, 0, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96,
        104, 104, 104, 104, 104, 104, 104, 104, 112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 112, 128, 112, 120, 96, 0, 0, 0,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104,
        112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 120, 136, 120, 128, 104, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96,
        104, 104, 104, 104, 104, 104, 104, 104, 112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 112, 128, 112, 120, 96, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96,
        104, 104, 104, 104, 104, 104, 104, 104, 112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 112, 128, 112, 120, 96, 0, 0, 0,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104,
        112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 120, 136, 120, 128, 104, 0, 0, 0,
        80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112,
        120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 136, 136, 136, 136, 136, 136, 136, 136, 128, 144, 128, 136, 112, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96,
        104, 104, 104, 104, 104, 104, 104, 104, 112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 112, 128, 112, 120, 96, 0, 0, 0,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104,
        112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 120, 136, 120, 128, 104, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96,
        104, 104, 104, 104, 104, 104, 104, 104, 112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 112, 128, 112, 120, 96, 0, 0, 0,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104,
        112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 120, 136, 120, 128, 104, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96,
        104, 104, 104, 104, 104, 104, 104, 104, 112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 112, 128, 112, 120, 96, 0, 0, 0,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104,
        112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 120, 136, 120, 128, 104, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96,
        104, 104, 104, 104, 104, 104, 104, 104, 112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 112, 128, 112, 120, 96, 0, 0, 0,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104,
        112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 120, 136, 120, 128, 104, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96,
        104, 104, 104, 104, 104, 104, 104, 104, 112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 112, 128, 112, 120, 96, 0, 0, 0,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104,
        112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 120, 136, 120, 128, 104, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96, 96,
        104, 104, 104, 104, 104, 104, 104, 104, 112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 112, 128, 112, 120, 96, 0, 0, 0,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104,
        112, 112, 112, 112, 112, 112, 112, 112, 120, 120, 120, 120, 120, 120, 120, 120, 128, 128, 128, 128, 128, 128, 128, 128, 120, 136, 120, 128, 104, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 96, 104, 80, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 88, 104, 88, 96, 72, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 32, 48, 32, 48, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 32, 48, 32, 48, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 32, 48, 32, 48, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        48, 48, 48, 48, 48, 48, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
        72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 88, 88, 88, 88, 88, 88, 88, 88, 80, 96, 80, 88, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 32, 48, 32, 48, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 80, 80, 80, 80, 80, 80, 80, 80, 64, 80, 64, 80, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 32, 48, 32, 48, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 0, 0, 16, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 32, 48, 32, 48, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 32, 48, 32, 48, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 48, 48, 48, 48, 48, 48, 48, 48,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 0, 16, 16, 80, 0, 64, 0, 80, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 72, 72, 72, 72, 72, 72, 72, 72, 88, 88, 88, 88, 88, 88, 88, 88, 72, 80, 72, 88, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 40, 40, 40, 40, 40, 40, 40, 40, 56, 56, 56, 56, 56, 56, 56, 56, 40, 48, 40, 56, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 32, 48, 32, 48, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 56, 72, 56, 64, 40, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 56, 72, 56, 64, 40, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 56, 72, 56, 64, 40, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 56, 72, 56, 64, 40, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 56, 72, 56, 64, 40, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 56, 72, 56, 64, 40, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 56, 72, 56, 64, 40, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40,
        48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 56, 72, 56, 64, 40, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 56, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 24, 24, 24, 24, 24, 24, 24, 24, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 24, 24, 24, 24, 24, 24, 24, 24, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 24, 24, 24, 24, 24, 24, 24, 24, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 24, 24, 24, 24, 24, 24, 24, 24, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 24, 24, 24, 24, 24, 24, 24, 24, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 24, 24, 24, 24, 24, 24, 24, 24, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 24, 24, 24, 24, 24, 24, 24, 24, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 24, 24, 24, 24, 24, 24, 24, 24, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        24, 24, 24, 24, 24, 24, 24, 24, 32, 32, 32, 32, 32, 32, 32, 32, 40, 40, 40, 40, 40, 40, 40, 40, 32, 48, 32, 40, 16, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 0, 0, 0, 0, 0, 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        40, 40, 40, 40, 40, 40, 40, 40, 48, 48, 48, 48, 48, 48, 48, 48, 56, 56, 56, 56, 56, 56, 56, 56, 48, 64, 48, 56, 32, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 64, 72, 48, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 72, 72, 72, 72, 72, 72, 72, 72, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 120, 120, 120, 120, 120, 120, 120, 120, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80,
        88, 88, 88, 88, 88, 88, 88, 88, 96, 96, 96, 96, 96, 96, 96, 96, 104, 104, 104, 104, 104, 104, 104, 104, 96, 112, 0, 0, 0, 0, 0, 0,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
        64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 80, 80, 80, 80, 80, 80, 80, 80, 72, 88, 72, 80, 64, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
        56, 56, 56, 56, 56, 56, 56, 56, 64, 64, 64, 64, 64, 64, 64, 64, 72, 72, 72, 72, 72, 72, 72, 72, 64, 80, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
        16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
    ];
    private static ReadOnlySpan<byte> m68ki_shift_8_table =>
    [
        0x00, 0x80, 0xc0, 0xe0, 0xf0, 0xf8, 0xfc, 0xfe, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        0xff,
    ];
    private static ReadOnlySpan<ushort> m68ki_shift_16_table =>
    [
        0x0000, 0x8000, 0xc000, 0xe000, 0xf000, 0xf800, 0xfc00, 0xfe00, 0xff00, 0xff80, 0xffc0, 0xffe0, 0xfff0, 0xfff8, 0xfffc, 0xfffe, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff,
        0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff,
        0xffff,
    ];
    private static ReadOnlySpan<uint> m68ki_shift_32_table =>
    [
        0x00000000, 0x80000000, 0xc0000000, 0xe0000000, 0xf0000000, 0xf8000000, 0xfc000000, 0xfe000000, 0xff000000, 0xff800000, 0xffc00000, 0xffe00000, 0xfff00000, 0xfff80000, 0xfffc0000, 0xfffe0000, 0xffff0000, 0xffff8000, 0xffffc000, 0xffffe000, 0xfffff000, 0xfffff800, 0xfffffc00, 0xfffffe00, 0xffffff00, 0xffffff80, 0xffffffc0, 0xffffffe0, 0xfffffff0, 0xfffffff8, 0xfffffffc, 0xfffffffe,
        0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff,
        0xffffffff,
    ];
    private static ReadOnlySpan<ushort> m68ki_exception_cycle_table_base =>
    [
        40, 4, 50, 50, 34, 38, 38, 34, 34, 34, 34, 34, 4, 4, 4, 44, 4, 4, 4, 4, 4, 4, 4, 4, 44, 44, 44, 44, 44, 44, 44, 44,
        34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
    ];
    private static ReadOnlySpan<uint> m68ki_cycle_interrupts_base =>
    [
        50, 59, 58, 57, 56, 55, 54, 53, 52, 51,
    ];
}
