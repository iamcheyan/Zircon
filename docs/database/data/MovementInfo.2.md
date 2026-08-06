<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改。重新生成: dotnet run --project Tools/SystemDbProbe -- --dump docs/database -->

# 传送点（MovementInfo）

> 记录 #2788 – #3048，共 554 条（第 2/2 部分）。

[README](../README.md) · [← 上一部分](MovementInfo.1.md)

## 快速浏览

| # | SourceRegion | DestinationRegion | Icon | NeedItem | RequiredClass |
|---|---|---|---|---|---|
| 2788 | D1505 (#81) / Row 3 Fake Bottom Doors (#1024) | D1505 (#81) / Row 2 Top Landing (#1022) | None | — | All |
| 2789 | D1505 (#81) / Row 3 Fake Top Doors (#1023) | D1505 (#81) / Row 2 Bottom Landing (#1021) | None | — | All |
| 2790 | D1505 (#81) / Row 3 Real Bottom door (#1075) | D1505 (#81) / Row 4 Top Landing (#1030) | None | — | All |
| 2791 | D1505 (#81) / Row 3 Real Top door (#1076) | D1505 (#81) / Row 4 Bottom Landing (#1029) | None | — | All |
| 2792 | D1505 (#81) / Row 4 Fake Bottom Doors (#1027) | D1505 (#81) / Row 3 Top Landing (#1026) | None | — | All |
| 2793 | D1505 (#81) / Row 4 Fake Top Doors (#1028) | D1505 (#81) / Row 3 Bottom Landing (#1025) | None | — | All |
| 2794 | D1505 (#81) / Row 4 Real Left Bottom door (#1077) | D1505 (#81) / Row 5 Left Top Landing (#1034) | None | — | All |
| 2795 | D1505 (#81) / Row 4 Real Left Top door (#1078) | D1505 (#81) / Row 5 Left Bottom Landing (#1033) | None | — | All |
| 2796 | D1505 (#81) / Row 4 Real Right Bottom door (#1099) | D1505 (#81) / Row 5 Right Top Landing (#1054) | None | — | All |
| 2797 | D1505 (#81) / Row 4 Real Right Top door (#1100) | D1505 (#81) / Row 5 Right Bottom Landing (#1053) | None | — | All |
| 2798 | D1505 (#81) / Row 5 Left Fake Bottom Doors (#1031) | D1505 (#81) / Row 4 Top Landing (#1030) | None | — | All |
| 2799 | D1505 (#81) / Row 5 Left Fake Top Doors (#1032) | D1505 (#81) / Row 4 Bottom Landing (#1029) | None | — | All |
| 2800 | D1505 (#81) / Row 5 Left Real Bottom door (#1079) | D1505 (#81) / Row 6 Left Top Landing (#1038) | None | — | All |
| 2801 | D1505 (#81) / Row 5 Left Real Top door (#1080) | D1505 (#81) / Row 6 Left Bottom Landing (#1037) | None | — | All |
| 2802 | D1505 (#81) / Row 5 Right Fake Bottom Doors (#1051) | D1505 (#81) / Row 4 Top Landing (#1030) | None | — | All |
| 2803 | D1505 (#81) / Row 5 Right Fake Top Doors (#1052) | D1505 (#81) / Row 4 Bottom Landing (#1029) | None | — | All |
| 2804 | D1505 (#81) / Row 5 Right Real Bottom door (#1089) | D1505 (#81) / Row 6 Right Top Landing (#1058) | None | — | All |
| 2805 | D1505 (#81) / Row 5 Right Real Top door (#1090) | D1505 (#81) / Row 6 Right Bottom Landing (#1057) | None | — | All |
| 2806 | D1505 (#81) / Row 6 Left Fake Bottom Doors (#1035) | D1505 (#81) / Row 5 Left Top Landing (#1034) | None | — | All |
| 2807 | D1505 (#81) / Row 6 Left Fake Top Doors (#1036) | D1505 (#81) / Row 5 Left Bottom Landing (#1033) | None | — | All |
| 2808 | D1505 (#81) / Row 6 Left Real Bottom door (#1081) | D1505 (#81) / Row 7 Left Top Landing (#1042) | None | — | All |
| 2809 | D1505 (#81) / Row 6 Left Real Top door (#1082) | D1505 (#81) / Row 7 Left Bottom Landing (#1041) | None | — | All |
| 2810 | D1505 (#81) / Row 6 Right Fake Bottom Doors (#1055) | D1505 (#81) / Row 5 Right Top Landing (#1054) | None | — | All |
| 2811 | D1505 (#81) / Row 6 Right Fake Top Doors (#1056) | D1505 (#81) / Row 5 Right Bottom Landing (#1053) | None | — | All |
| 2812 | D1505 (#81) / Row 6 Right Real Bottom door (#1091) | D1505 (#81) / Row 7 Right Top Landing (#1062) | None | — | All |
| 2813 | D1505 (#81) / Row 6 Right Real Top door (#1092) | D1505 (#81) / Row 7 Right Bottom Landing (#1061) | None | — | All |
| 2814 | D1505 (#81) / Row 7 Left Fake Bottom Doors (#1039) | D1505 (#81) / Row 6 Left Top Landing (#1038) | None | — | All |
| 2815 | D1505 (#81) / Row 7 Left Fake Top Doors (#1040) | D1505 (#81) / Row 6 Left Bottom Landing (#1037) | None | — | All |
| 2816 | D1505 (#81) / Row 7 Left Real Bottom door (#1083) | D1505 (#81) / Row 8 Left Top Landing (#1046) | None | — | All |
| 2817 | D1505 (#81) / Row 7 Left Real Top door (#1084) | D1505 (#81) / Row 8 Left Bottom Landing (#1045) | None | — | All |
| 2818 | D1505 (#81) / Row 7 Right Fake Bottom Doors (#1059) | D1505 (#81) / Row 6 Right Top Landing (#1058) | None | — | All |
| 2819 | D1505 (#81) / Row 7 Right Fake Top Doors (#1060) | D1505 (#81) / Row 6 Right Bottom Landing (#1057) | None | — | All |
| 2820 | D1505 (#81) / Row 7 Right Real Bottom door (#1093) | D1505 (#81) / Row 8 Right Top Landing (#1066) | None | — | All |
| 2821 | D1505 (#81) / Row 7 Right Real Top door (#1094) | D1505 (#81) / Row 8 Right Bottom Landing (#1065) | None | — | All |
| 2822 | D1505 (#81) / Row 8 Left Fake Bottom Doors (#1043) | D1505 (#81) / Row 7 Left Top Landing (#1042) | None | — | All |
| 2823 | D1505 (#81) / Row 8 Left Fake Top Doors (#1044) | D1505 (#81) / Row 7 Left Bottom Landing (#1041) | None | — | All |
| 2824 | D1505 (#81) / Row 8 Left Real Bottom door (#1085) | D1505 (#81) / Row 9 Left Top Landing (#1050) | None | — | All |
| 2825 | D1505 (#81) / Row 8 Left Real Top door (#1086) | D1505 (#81) / Row 9 Left Bottom Landing (#1049) | None | — | All |
| 2826 | D1505 (#81) / Row 8 Right Fake Bottom Doors (#1063) | D1505 (#81) / Row 7 Right Top Landing (#1062) | None | — | All |
| 2827 | D1505 (#81) / Row 8 Right Fake Top Doors (#1064) | D1505 (#81) / Row 7 Right Bottom Landing (#1061) | None | — | All |
| 2828 | D1505 (#81) / Row 8 Right Real Bottom door (#1095) | D1505 (#81) / Row 9 Right Top Landing (#1070) | None | — | All |
| 2829 | D1505 (#81) / Row 8 Right Real Top door (#1096) | D1505 (#81) / Row 9 Right Bottom Landing (#1069) | None | — | All |
| 2830 | D1505 (#81) / Row 9 Left Fake Bottom Doors (#1047) | D1505 (#81) / Row 8 Left Top Landing (#1046) | None | — | All |
| 2831 | D1505 (#81) / Row 9 Left Fake Top Doors (#1048) | D1505 (#81) / Row 8 Left Bottom Landing (#1045) | None | — | All |
| 2832 | D1505 (#81) / Row 9 Left Real Bottom door (#1087) | D1505 (#81) / Row 5 Right Top Landing (#1054) | None | — | All |
| 2833 | D1505 (#81) / Row 9 Left Real Top door (#1088) | D1505 (#81) / Row 5 Right Bottom Landing (#1053) | None | — | All |
| 2834 | D1505 (#81) / Row 9 Right Fake Bottom Doors (#1067) | D1505 (#81) / Row 8 Right Top Landing (#1066) | None | — | All |
| 2835 | D1505 (#81) / Row 9 Right Fake Top Doors (#1068) | D1505 (#81) / Row 8 Right Bottom Landing (#1065) | None | — | All |
| 2836 | D1505 (#81) / Row 9 Right Real Bottom door (#1097) | D1505 (#81) / Row 5 Left Top Landing (#1034) | None | — | All |
| 2837 | D1505 (#81) / Row 9 Right Real Top door (#1098) | D1505 (#81) / Row 5 Left Bottom Landing (#1033) | None | — | All |
| 2838 | D15032 (#77) / Top Door (#958) | D1502 (#75) / Floor 3 Top Landing (#944) | Up | — | All |
| 2839 | D15031 (#76) / Right Door (#953) | D1502 (#75) / Floor 3 Right Landing (#946) | Up | — | All |
| 2840 | D15034 (#79) / Bottom Door (#968) | D1502 (#75) / Floor 3 Bottom Landing (#948) | Up | — | All |
| 2841 | D15033 (#78) / Left Door (#963) | D1502 (#75) / Floor 3 Left Landing (#950) | Up | — | All |
| 2842 | 12 (#292) / Departed Valley Door (#1126) | D2501 (#298) / Entrance Landing (#1130) | Cave | — | All |
| 2843 | D2501 (#298) / Entrance Door (#1129) | 12 (#292) / Departed Valley Landing (#1127) | Exit | — | All |
| 2844 | D2501 (#298) / Floor 2 Door (#1131) | D2502 (#299) / Floor 1 Landing (#1135) | Down | — | All |
| 2845 | D2502 (#299) / Floor 1 Door (#1134) | D2501 (#298) / Floor 2 Landing (#1132) | Up | — | All |
| 2846 | D2502 (#299) / Floor 3 Door (#1136) | D2503 (#300) / Floor 2 Landing (#1140) | Down | — | All |
| 2847 | D2503 (#300) / Floor 2 Door (#1139) | D2502 (#299) / Floor 3 Landing (#1137) | Up | — | All |
| 2848 | D2503 (#300) / Banyo Island Door (#1141) | 13 (#293) / Departed Cave Landing (#1146) | Exit | — | All |
| 2849 | 13 (#293) / Departed Cave Door (#1145) | D2503 (#300) / Banyo Island Landing (#1142) | Cave | — | All |
| 2850 | 13 (#293) / Banyo Cave Door (#1147) | D2601 (#301) / Landing (#1151) | Cave | — | All |
| 2851 | D2601 (#301) / Door (#1150) | 13 (#293) / Banyo Cave Landing (#1148) | Exit | — | All |
| 2852 | D002 (#13) / Jinchon Door (#234) | D1200 (#42) / Desert Landing (#1196) | Cave | — | All |
| 2853 | 5 (#9) / Jinchon Door (#206) | D1200 (#42) / Mud Wall Landing (#1198) | Cave | — | All |
| 2854 | D1200 (#42) / Desert Door (#1195) | D002 (#13) / Jinchon Landing (#235) | Exit | — | All |
| 2855 | D1200 (#42) / Mud Wall Door (#1197) | 5 (#9) / Junchon Landing (#207) | Exit | — | All |
| 2856 | D1200 (#42) / Floor 2 Door - N (#1199) | D12014 (#47) / Floor 1 Landing (#1222) | Down | — | All |
| 2857 | D1200 (#42) / Floor 2 Door - E (#1201) | D12013 (#46) / Floor 1 Landing (#1217) | Down | — | All |
| 2858 | D1200 (#42) / Floor 2 Door - S (#1203) | D12012 (#45) / Floor 1 Landing (#1214) | Down | — | All |
| 2859 | D1200 (#42) / Floor 2 Door - W (#1205) | D12011 (#43) / Floor 1 Landing (#1209) | Down | — | All |
| 2860 | D12011 (#43) / Floor 1 Door (#1208) | D1200 (#42) / Floor 2 Landing - W (#1206) | Up | — | All |
| 2861 | D12011 (#43) / Floor 3 Door (#1210) | D12021 (#48) / Floor 2 Landing (#1227) | Down | — | All |
| 2862 | D12012 (#45) / Floor 1 Door (#1213) | D1200 (#42) / Floor 2 Landing - S (#1204) | Up | — | All |
| 2863 | D12013 (#46) / Floor 1 Door (#1216) | D1200 (#42) / Floor 2 Landing - E (#1202) | Up | — | All |
| 2864 | D12013 (#46) / Floor 3 Door (#1218) | D12023 (#50) / Floor 2 Landing (#1237) | Down | — | All |
| 2865 | D12014 (#47) / Floor 1 Door (#1221) | D1200 (#42) / Floor 2 Landing - N (#1200) | Down | — | All |
| 2866 | D12014 (#47) / Floor 3 Door (#1223) | D12024 (#51) / Floor 2 Landing (#1242) | Up | — | All |
| 2867 | D12021 (#48) / Floor 2 Door (#1226) | D12011 (#43) / Floor 3 Landing (#1211) | Down | — | All |
| 2868 | D12021 (#48) / Floor 3 Door - S (#1228) | D12022 (#49) / Floor 3 Landing - W (#1232) | Down | — | All |
| 2869 | D12022 (#49) / Floor 3 Door - W (#1231) | D12021 (#48) / Floor 3 Landing - S (#1229) | Down | — | All |
| 2870 | D12022 (#49) / Floor 4 Door - S (#1233) | D12031 (#52) / Floor 3 Landing (#1250) | Down | — | All |
| 2871 | D12023 (#50) / Floor 2 Door (#1236) | D12013 (#46) / Floor 3 Landing (#1219) | Up | — | All |
| 2872 | D12023 (#50) / Floor 4 Door (#1238) | D12032 (#53) / Floor 3 Landing (#1255) | Down | — | All |
| 2873 | D12024 (#51) / Floor 2 Door (#1241) | D12014 (#47) / Floor 3 Landing (#1224) | Up | — | All |
| 2874 | D12024 (#51) / Floor 4 Door (#1243) | D12033 (#54) / Floor 3 Landing (#1247) | Down | — | All |
| 2875 | D12031 (#52) / Floor 3 Door (#1249) | D12022 (#49) / Floor 4 Landing - S (#1234) | Up | — | All |
| 2876 | D12031 (#52) / Floor 4 Door - E (#1251) | D12032 (#53) / Floor 4 Landing - S (#1257) | Down | — | All |
| 2877 | D12032 (#53) / Floor 3 Door (#1254) | D12023 (#50) / Floor 4 Landing (#1239) | Up | — | All |
| 2878 | D12032 (#53) / Floor 4 Door - S (#1256) | D12031 (#52) / Floor 4 Landing - E (#1252) | Up | — | All |
| 2879 | D12032 (#53) / Floor 5 Door (#1258) | D12041 (#55) / Floor 4 Landing (#1262) | Down | — | All |
| 2880 | D12033 (#54) / Floor 3 Door (#1246) | D12024 (#51) / Floor 4 Landing (#1244) | Up | — | All |
| 2881 | D12041 (#55) / Floor 4 Door (#1261) | D12032 (#53) / Floor 5 Landing (#1259) | Up | — | All |
| 2882 | D12041 (#55) / Floor 5 Door - N (#1263) | D12042 (#56) / Floor 5 Landing - E (#1267) | Down | — | All |
| 2883 | D12042 (#56) / Floor 5 Door - E (#1266) | D12041 (#55) / Floor 5 Landing - N (#1264) | Up | — | All |
| 2884 | D12042 (#56) / Floor 6 Door (#1268) | D1205 (#57) / Floor 5 Landing (#1272) | Down | — | All |
| 2885 | D1205 (#57) / Floor 5 Door (#1271) | D12042 (#56) / Floor 6 Landing (#1269) | Up | — | All |
| 2888 | D1205 (#57) / Floor 7 Door (#1273) | D1206 (#58) / Landing (#1277) | Down | — | All |
| 2889 | D002 (#13) / Black Palace Door (#240) | D1301 (#62) / Desert Landing (#1161) | Cave | — | All |
| 2890 | 5 (#9) / Black Palace Door (#208) | D1301 (#62) / Mud Wall Landing (#1163) | Cave | — | All |
| 2891 | D1301 (#62) / Desert Door (#1160) | D002 (#13) / Black Palance Landing (#241) | Exit | — | All |
| 2892 | D1301 (#62) / Mud Wall Door (#1162) | 5 (#9) / Black Palace Landing (#209) | Exit | — | All |
| 2893 | D1301 (#62) / Floor 2 Door W (#1164) | D13021 (#63) / Floor 1 Landing (#1170) | Down | — | All |
| 2894 | D1301 (#62) / Floor 2 Door E (#1166) | D13022 (#64) / Floor 1 Landing (#1175) | Down | — | All |
| 2895 | D13021 (#63) / Floor 1 Door (#1169) | D1301 (#62) / Floor 2 Landing W (#1165) | Up | — | All |
| 2896 | D13021 (#63) / Floor 3 Door (#1171) | D1303 (#65) / Floor 2 Landing - W (#1180) | Down | — | All |
| 2897 | D13022 (#64) / Floor 1 Door (#1174) | D1301 (#62) / Floor 2 Landing E (#1167) | Up | — | All |
| 2898 | D13022 (#64) / Floor 3 Door (#1176) | D1303 (#65) / Floor 2 Landing - E (#1182) | Down | — | All |
| 2899 | D1303 (#65) / Floor 2 Door - W (#1179) | D13021 (#63) / Floor 3 Landing (#1172) | Up | — | All |
| 2900 | D1303 (#65) / Floor 2 Door - E (#1181) | D13022 (#64) / Floor 3 Landing (#1177) | Up | — | All |
| 2901 | D1303 (#65) / Floor 4 Door (#1183) | D1304 (#66) / Floor 3 Landing (#1187) | Down | — | All |
| 2902 | D1304 (#66) / Floor 5 Door (#1188) | D1305 (#67) / Landing (#1192) | Up | — | All |
| 2904 | D112 (#40) / Floor 1 Door (#529) | D111 (#39) / Floor 2 Landing (#523) | Up | — | All |
| 2905 | 14_000 (#459) / Door (#1279) | 0 (#1) / Assassin's Hideout Landing (#1280) | None | — | All |
| 2906 | D1104 (#36) / Traps (#1158) | D1104 (#36) / Whole Map (#407) | None | — | All |
| 2907 | D1105 (#37) / Traps (#1157) | D1105 (#37) / Whole Map (#413) | None | — | All |
| 2908 | D1402 (#69) / Traps (#1153) | D1401 (#68) / Teleport Area (#711) | None | — | All |
| 2909 | D1403 (#70) / Traps (#1154) | D1401 (#68) / Teleport Area (#711) | None | — | All |
| 2910 | D1404 (#71) / Traps (#1155) | D1401 (#68) / Teleport Area (#711) | None | — | All |
| 2911 | D1405 (#72) / Traps (#1156) | D1401 (#68) / Teleport Area (#711) | None | — | All |
| 2912 | D301 (#139) / Floor 2 Door (#648) | D302 (#140) / Floor 1 Landing (#655) | Down | — | All |
| 2913 | D302 (#140) / Floor 1 Door (#654) | D301 (#139) / Floor 2 Landing (#649) | Up | — | All |
| 2914 | D302 (#140) / Floor 3 Door (#656) | D303 (#141) / Floor 2 Landing (#662) | Down | — | All |
| 2915 | D303 (#141) / Floor 2 Door (#661) | D302 (#140) / Floor 3 Landing (#657) | Up | — | All |
| 2916 | 0_001 (#3) / Door (#40) | 0_000 (#2) / Left Landing (#37) | Building | — | All |
| 2917 | 0_002 (#4) / Door (#42) | 0_000 (#2) / Right Landing (#39) | Building | — | All |
| 2918 | D1405 (#72) / Exit Door (#738) | D1401 (#68) / Teleport Area (#711) | Exit | — | All |
| 2919 | 4 (#8) / Southern Dunes - Door (#186) | D4000 (#587) / Numa Village - Landing (#1324) | Province | — | All |
| 2920 | D4000 (#587) / Numa Village - Door (#1323) | 4 (#8) / Southern Dunes - Landing (#187) | Province | — | All |
| 2921 | D4000 (#587) / Southern Wastes - Door (#1325) | D4001 (#588) / Southern Dunes - Landing (#1329) | Province | — | All |
| 2922 | D4001 (#588) / Southern Dunes - Door (#1328) | D4000 (#587) / Southern Wastes - Landing (#1326) | Province | — | All |
| 2923 | D4001 (#588) / Southern Coast - Door (#1330) | D4002 (#589) / Southern Wastes - Landing (#1334) | Province | — | All |
| 2924 | D4002 (#589) / Southern Wastes - Door (#1333) | D4001 (#588) / Southern Coast - Landing (#1331) | Province | — | All |
| 2925 | D4002 (#589) / Southern Check Point - Door (#1335) | D4003 (#590) / Southern Coast - Landing (#1441) | Province | — | All |
| 2926 | D4003 (#590) / Beyond Shore - Door (#1340) | 16_001 (#568) / Southern Check Point - Landing (#1343) | Province | — | All |
| 2927 | D4003 (#590) / Southern Coast - Door (#1440) | D4002 (#589) / Southern Check Point - Landing (#1336) | Province | — | All |
| 2928 | 16_001 (#568) / Southern Check Point - Door (#1342) | D4003 (#590) / Beyond Shore - Landing (#1341) | Province | — | All |
| 2929 | 16_001 (#568) / Southern Wall - Door (#1407) | D4101 (#591) / Beyond Shore - Landing (#1345) | Province | — | All |
| 2930 | 16_001 (#568) / Western Coast - Door (#1442) | 16_002 (#569) / Beyond Shore - Landing (#1348) | Province | — | All |
| 2931 | 16_002 (#569) / Beyond Shore - Door (#1347) | 16_001 (#568) / Western Coast - Landing (#1443) | Province | — | All |
| 2932 | 16_002 (#569) / Western Pass - Door (#1349) | 16_003 (#570) / Western Coast - Landing (#1353) | Province | — | All |
| 2933 | 16_003 (#570) / Western Coast - Door (#1352) | 16_002 (#569) / Western Pass - Landing (#1350) | Province | — | All |
| 2934 | 16_003 (#570) / Western Arids - Door (#1364) | 16 (#567) / Western Pass - Landing (#1355) | Province | — | All |
| 2935 | 16 (#567) / Lost Oasis - Door 1 (#1356) | 17 (#571) / Western Arids - Landing 1 (#1368) | Province | — | All |
| 2936 | 16 (#567) / Lost Oasis - Door 2 (#1358) | 17 (#571) / Western Arids - Landing 2 (#1370) | Province | — | All |
| 2937 | 16 (#567) / Lost Oasis - Door 3 (#1360) | 17 (#571) / Western Arids - Landing 3 (#1372) | Province | — | All |
| 2938 | 16 (#567) / Lost Oasis - Door 4 (#1362) | 17 (#571) / Western Arids - Landing 4 (#1374) | Province | — | All |
| 2939 | 17 (#571) / Western Arids - Door 1 (#1367) | 16 (#567) / Lost Oasis - Landing 1 (#1357) | Province | — | All |
| 2940 | 17 (#571) / Western Arids - Door 2 (#1369) | 16 (#567) / Lost Oasis - Landing 2 (#1359) | Province | — | All |
| 2941 | 17 (#571) / Western Arids - Door 3 (#1371) | 16 (#567) / Lost Oasis - Landing 3 (#1361) | Province | — | All |
| 2942 | 17 (#571) / Western Arids - Door 4 (#1373) | 16 (#567) / Lost Oasis - Landing 4 (#1363) | Province | — | All |
| 2943 | 17 (#571) / Arid Flats - Door (#1375) | 18 (#572) / Lost Oasis - Landing (#1379) | Province | — | All |
| 2944 | 18 (#572) / Lost Oasis - Door (#1378) | 17 (#571) / Aird Flats - Landing (#1376) | Province | — | All |
| 2945 | 18 (#572) / Quartz Mine - Door (#1380) | ID7_000 (#593) / Arid Flats - Landing (#1384) | Cave | — | All |
| 2946 | ID7_000 (#593) / Arid Flats - Door (#1383) | 18 (#572) / Quartz Mine - Landing (#1381) | Exit | — | All |
| 2947 | ID7_000 (#593) / Quartz Mine Lv 2 - Door (#1386) | ID7_001 (#594) / Quartz Mine Lv 1 - Landing (#1389) | Down | Pure Quartz (#827) | All |
| 2948 | ID7_001 (#594) / Quartz Mine Lv 1 - Door (#1388) | ID7_000 (#593) / Quartz Mine Lv 2 - Landing (#1387) | Up | Pure Quartz (#827) | All |
| 2949 | ID7_001 (#594) / Quartz Mine Lv 3 - Door (#1390) | ID7_002 (#595) / Quartz Mine Lv 2 - Landing (#1394) | Down | — | All |
| 2950 | ID7_002 (#595) / Quartz Mine Lv 2 - Door (#1393) | ID7_001 (#594) / Quartz Mine Lv 3 - Landing (#1391) | Up | — | All |
| 2951 | ID7_002 (#595) / Quartz Mine Lv 4 - Door (#1395) | ID7_003 (#596) / Quartz Mine Lv 3 - Landing (#1399) | Down | — | All |
| 2952 | ID7_003 (#596) / Quartz Mine Lv 3 - Door (#1398) | ID7_002 (#595) / Quartz Mine Lv 4 - Landing (#1396) | Up | — | All |
| 2953 | ID7_003 (#596) / Quartz Mine Lv 5 - Door (#1400) | ID7_004 (#597) / Quartz Mine Lv 4 - Landing (#1404) | Down | — | All |
| 2955 | D4101 (#591) / Beyond Shore - Door (#1344) | 16_001 (#568) / Southern Wall - Landing (#1408) | Province | — | All |
| 2956 | D4101 (#591) / Lost Way - Door (#1444) | D4102 (#592) / Southern Wall - Landing (#1412) | Province | — | All |
| 2957 | D4102 (#592) / Southern Wall - Door (#1411) | D4101 (#591) / Lost Way - Landing (#1445) | Province | — | All |
| 2958 | D4102 (#592) / Lost Village - Door (#1446) | 19 (#573) / Lost Way - Landing (#1417) | Province | — | All |
| 2959 | 19 (#573) / Lost Way - Door (#1416) | D4102 (#592) / Lost Village - Landing (#1447) | Province | — | All |
| 2960 | 19 (#573) / Lost Pass - Door (#1419) | 19_1 (#574) / Lost Village - Landing (#1422) | Province | — | All |
| 2961 | 19_1 (#574) / Lost Village - Door (#1421) | 19 (#573) / Lost Pass - Landing (#1420) | Province | — | All |
| 2962 | 19_1 (#574) / Abandoned Town - Door (#1423) | ID9_00 (#598) / Lost Pass - Landing (#1426) | Province | — | All |
| 2963 | ID9_00 (#598) / Lost Pass - Door (#1425) | 19_1 (#574) / Abandoned Town - Landing (#1424) | Province | — | All |
| 2964 | ID9_00 (#598) / Forgotton Monastery - Door (#1427) | ID9_01 (#599) / Abandoned Town - Landing (#1431) | Cave | — | All |
| 2965 | ID9_01 (#599) / Abandoned Town - Door (#1430) | ID9_00 (#598) / Forgotton Monastery - Landing (#1428) | Exit | — | All |
| 2966 | ID9_01 (#599) / Forgotton Monastery Lv 2 - Door (#1432) | ID9_02 (#600) / Forgotton Monastery Lv 1 - Landing (#1436) | Down | — | All |
| 2967 | ID9_02 (#600) / Forgotton Monastery Lv 1 - Door (#1435) | ID9_01 (#599) / Forgotton Monastery Lv 2 - Landing (#1433) | Up | — | All |
| 2968 | 16 (#567) / Western Pass - Door (#1354) | 16_003 (#570) / Western Arids - Landing (#1365) | Province | — | All |
| 2969 | 1 (#5) / Unknown Province Door (#49) | 11 (#291) / Lost Paradise - Landing (#1450) | Province | — | All |
| 2970 | 11 (#291) / Lost Paradise - Door (#1449) | 1 (#5) / Unknown Province Landing (#50) | Province | — | All |
| 2971 | 11 (#291) / Hyunmoon Temple - Door (#1451) | D2401 (#294) / Taoist Temple - Landing (#1477) | Cave | — | All |
| 2972 | D2401 (#294) / Taoist Temple - Door (#1476) | 11 (#291) / Hyunmoon Temple - Landing (#1452) | Exit | — | All |
| 2973 | D2401 (#294) / Hyunmoon Temple Lv 2 - Door (#1478) | D2402 (#295) / Hyunmoon Temple Lv 1 - Landing (#1482) | Down | — | All |
| 2974 | D2402 (#295) / Hyunmoon Temple Lv 1 - Door (#1481) | D2401 (#294) / Hyunmoon Temple Lv 2 - Landing (#1479) | Up | — | All |
| 2975 | D2402 (#295) / Hyunmoon Temple Lv 3 - Door (#1483) | D2403 (#296) / Hyunmoon Temple Lv 2 - Landing (#1487) | Down | — | All |
| 2976 | D2403 (#296) / Hyunmoon Temple Lv 2 - Door (#1486) | D2402 (#295) / Hyunmoon Temple Lv 3 - Landing (#1484) | Up | — | All |
| 2977 | 0 (#1) / Bichon Castle Entrance (#6) | 10 (#259) / Bichon Town - Landing (#1492) | Province | — | All |
| 2978 | 10 (#259) / Bichon Town - Door (#1491) | 0 (#1) / Bichon Castle Landing (#7) | Province | — | All |
| 2979 | 10 (#259) / Goru Cave - Door (#1493) | D2301 (#44) / Bichon Castle - Landing (#1514) | Cave | — | All |
| 2980 | D2301 (#44) / Bichon Castle - Door (#1513) | 10 (#259) / Goru Cave - Landing (#1494) | Exit | — | All |
| 2981 | D2301 (#44) / Goru Cave Lv 2 - Door (#1515) | D2302 (#260) / Goru Cave Lv 1 - Landing (#1519) | Down | — | All |
| 2982 | D2302 (#260) / Goru Cave Lv 1 - Door (#1518) | D2301 (#44) / Goru Cave Lv 2 - Landing (#1516) | Up | — | All |
| 2983 | D2302 (#260) / Goru Cave Lv 3 - Door (#1520) | D2303 (#261) / Goru Cave Lv 2 - Landing (#1524) | Down | — | All |
| 2984 | D2303 (#261) / Goru Cave Lv 2 - Door (#1523) | D2302 (#260) / Goru Cave Lv 3 - Landing (#1521) | Up | — | All |
| 2985 | D2303 (#261) / Goru Cave Lv 4 - Door (#1525) | D2304 (#262) / Goru Cave Lv 3 - Landing (#1531) | Down | — | All |
| 2986 | D2304 (#262) / Goru Cave Lv 3 - Door (#1530) | D2303 (#261) / Goru Cave Lv 4 - Landing (#1526) | Up | — | All |
| 2988 | 7 (#11) / Cave Door (#217) | D1802 (#121) / Infernal Island - Landing (#1538) | Cave | — | All |
| 2989 | D1802 (#121) / Infernal Island - Entrance (#1537) | 7 (#11) / Cave Landing (#218) | Exit | — | All |
| 2990 | 8 (#241) / Holy Palace Door (#835) | 8_002 (#280) / Forst Village Landing (#1542) | Province | — | All |
| 2991 | 8_002 (#280) / Frost Village Door (#1541) | 8 (#241) / Holy Palace Landing (#836) | Province | — | All |
| 2992 | 8_002 (#280) / Holy Palace Lv 1 - Door (#1543) | D2201 (#219) / Holy Palace Landing (#1546) | Cave | — | All |
| 2993 | D2201 (#219) / Holy Palace Door (#1545) | 8_002 (#280) / Holy Palace Lv 1 - Landing (#1544) | Exit | — | All |
| 2994 | D2201 (#219) / Holy Palace Lv 2 - Door (#1547) | D22021 (#273) / Holy Palace Lv 1 - Landing (#1556) | Down | — | All |
| 2995 | D22021 (#273) / Holy Palace Lv 1 - Door (#1555) | D2201 (#219) / Holy Palace Lv 2 - Landing (#1548) | Up | — | All |
| 2996 | D22021 (#273) / Holy Palace Lv 3 - Door (#1557) | D2204 (#277) / Holy Palace Lv 2 - Landing (#1560) | Down | — | All |
| 2997 | D2204 (#277) / Holy Palace Lv 2 - Door (#1559) | D22021 (#273) / Holy Palace Lv 3 - Landing (#1558) | Up | — | All |
| 2998 | D2204 (#277) / Holy Palace Lv 4 - Door (#1561) | D2205 (#278) / Holy Palace Lv 3 - Landing (#1563) | Down | — | All |
| 2999 | D006 (#332) / Lava Area Lv 2 - Door (#1571) | D007 (#333) / Lava Area Lv 1 - Landing (#1575) | Down | — | All |
| 3000 | D007 (#333) / Lava Area Lv 1 - Door (#1574) | D006 (#332) / Lava Area Lv 2 - Landing (#1572) | Up | — | All |
| 3001 | D007 (#333) / The Lair Entrance - Door (#1576) | D2900 (#334) / Lava Area Lv 2 - Landing (#1580) | Down | — | All |
| 3002 | D2900 (#334) / Lava Area Lv 2 - Door (#1579) | D007 (#333) / The Lair Entrance - Landing (#1577) | Up | — | All |
| 3003 | D2900 (#334) / The Lair Lv 1 - Door (#1581) | D2901 (#335) / The Lair Entrance - Landing (#1585) | Down | — | All |
| 3004 | D2901 (#335) / Tne Lair Entrance - Door (#1584) | D2900 (#334) / The Lair Lv 1 - Landing (#1582) | Up | — | All |
| 3005 | D2901 (#335) / The Lair Lv 2 West - Door (#1586) | D2902 (#336) / The Lair Lv 1 West - Landing (#1592) | Down | — | All |
| 3006 | D2901 (#335) / The Lair Lv 2 East - Door (#1588) | D2902 (#336) / The Lair Lv 1 East - Landing (#1594) | Down | — | All |
| 3007 | D2902 (#336) / The Lair Lv 1 West - Door (#1591) | D2901 (#335) / The Lair Lv 2 West - Landing (#1587) | Up | — | All |
| 3008 | D2902 (#336) / The Lair Lv 1 East - Door (#1593) | D2901 (#335) / The Lair Lv 2 East - Landing (#1589) | Up | — | All |
| 3009 | D2902 (#336) / The Lair Lv 3 West - Door (#1595) | D2904 (#339) / The Lair Lv 2 West - Landing (#1603) | Down | — | All |
| 3010 | D2902 (#336) / The Lair Lv 3 East - Door (#1597) | D2904 (#339) / The Lair Lv 2 East - Landing (#1605) | Down | — | All |
| 3011 | D2904 (#339) / The Lair Lv 2 West - Door (#1602) | D2902 (#336) / The Lair Lv 3 West - Landing (#1596) | Up | — | All |
| 3012 | D2904 (#339) / The Lair Lv 2 East - Door (#1604) | D2902 (#336) / The Lair Lv 3 East - Landing (#1598) | Up | — | All |
| 3013 | D2904 (#339) / The Lair Lv 4 West - Door (#1606) | D29051 (#340) / The Lair Lv 3 - Landing (#1613) | Down | — | All |
| 3014 | D2904 (#339) / The Lair Lv 4 East - Door (#1608) | D29052 (#341) / The Lair Lv 3 - Landing (#1618) | Down | — | All |
| 3015 | D29051 (#340) / The Lair Lv 5 - Door (#1614) | D2906 (#342) / The Lair Lv 4 West - Landing (#1623) | Down | — | All |
| 3016 | D29052 (#341) / The Lair Lv 5 - Door (#1619) | D2906 (#342) / The Lair Lv 4 East - Landing (#1625) | Down | — | All |
| 3017 | D2906 (#342) / The Lair Lv 6 - Door (#1626) | D2907 (#344) / Landing (#1629) | Down | — | All |
| 3019 | D008 (#460) / Frost Village - Door (#1632) | 8 (#241) / Dragon Abyss Landing (#834) | Province | — | All |
| 3020 | D008 (#460) / Dragon Abyss Lv 1 - Door (#1634) | D3001 (#461) / Dragon Abyss Ent - Landing (#1637) | Cave | — | All |
| 3021 | D3001 (#461) / Dragon Abyss Ent - Door (#1636) | D008 (#460) / Dragon Abyss Lv 1 - Landing (#1635) | Exit | — | All |
| 3022 | D3001 (#461) / Dragon Abyss Lv 2 - Door (#1640) | D3002 (#462) / Dragon Abyss Lv 1 - Landing (#1643) | Down | — | All |
| 3023 | D3002 (#462) / Dragon Abyss Lv 1 - Door (#1642) | D3001 (#461) / Dragon Abyss Lv 2 - Landing (#1641) | Up | — | All |
| 3024 | D3002 (#462) / Dragon Abyss Lv 3- Door (#1644) | D3004 (#466) / Dragon Abyss Lv 2 - Landing (#1649) | Down | — | All |
| 3025 | D3004 (#466) / Dragon Abyss Lv 2 - Door (#1648) | D3002 (#462) / Dragon Abyss Lv 3 - Landing (#1645) | Up | — | All |
| 3026 | D3004 (#466) / Dragon Abyss Lv 4 - Door (#1650) | D3005 (#470) / Dragon Abyss Lv 3 - Landing (#1654) | Down | — | All |
| 3027 | D3005 (#470) / Dragon Abyss Lv 3 - Door (#1653) | D3004 (#466) / Dragon Abyss Lv 4 - Landing (#1651) | Up | — | All |
| 3028 | D3005 (#470) / Dragon Abyss Lv 5 NW - Door (#1655) | D3005_BH (#601) / Dragon Abyss 4th - Landing (#1675) | Down | — | All |
| 3029 | D3005 (#470) / Dragon Abyss Lv 5 NE - Door (#1657) | D3005_CR (#602) / Dragon Abyss 4th - Landing (#1678) | Down | — | All |
| 3030 | D3005 (#470) / Dragon Abyss Lv 5 SW - Door (#1659) | D3005_HM (#603) / Dragon Abyss 4th - Landing (#1681) | Down | — | All |
| 3031 | D3005 (#470) / Dragon Abyss Lv 5 SE - Door (#1661) | D3005_JJ (#604) / Dragon Abyss 4th - Landing (#1684) | Down | — | All |
| 3032 | D3005 (#470) / Dragon Abyss Lv 6 - Door (#1663) | D3006 (#480) / Dragon Abyss 4th - Landing (#1668) | Down | Ancestral Tablet Of Sama Mage (#954) | All |
| 3034 | 8 (#241) / Dragon Abyss Door (#833) | D008 (#460) / Frost Village  - Landing (#1633) | Province | — | All |
| 3035 | D3400 (#605) / Lost Land 2 - Left Door (#1695) | D3400_1 (#606) / Lost Land - Left Landing (#1700) | Province | — | All |
| 3036 | D3400 (#605) / Lost Land 2 - Right Door (#1697) | D3400_1 (#606) / Lost Land - Right Landing (#1702) | Province | — | All |
| 3037 | D3400_1 (#606) / Lost Land - Left Door (#1699) | D3400 (#605) / Lost Land 2 - Left Landing (#1696) | Province | — | All |
| 3038 | D3400_1 (#606) / Lost Land - Right Door (#1701) | D3400 (#605) / Lost Land 2 - Right Landing (#1698) | Province | — | All |
| 3039 | D3400_1 (#606) / Lost Land 3 - Door (#1715) | ER51_Ice (#607) / Lost Land 2 - Landing (#1714) | Province | — | All |
| 3040 | ER51_Ice (#607) / Lost Land 2 - Door (#1713) | D3400_1 (#606) / Lost Land 3 - Landing (#1716) | Province | — | All |
| 3041 | D4003 (#590) / The Wall 1 - Door (#1719) | ID3_014 (#608) / Southern Check Point - Landing (#1722) | Cave | — | All |
| 3042 | ID3_014 (#608) / Southern Check Point - Door (#1721) | D4003 (#590) / The Wall 1 - Landing (#1720) | Exit | — | All |
| 3043 | ID3_014 (#608) / The Wall 2 - Door (#1723) | ID3_024 (#609) / The Wall - Landing (#1726) | Down | — | All |
| 3044 | ID3_024 (#609) / The Wall - Door (#1725) | ID3_014 (#608) / The Wall 2 - Landing (#1724) | Up | — | All |
| 3045 | D3005_BH (#601) / Dragon Abyss 4th -  Door (#1674) | D3005 (#470) / Dragon Abyss Lv 5 NW - Landing (#1656) | Up | — | All |
| 3046 | D3005_CR (#602) / Dragon Abyss 4th -  Door (#1677) | D3005 (#470) / Dragon Abyss Lv 5 NE - Landing (#1658) | Up | — | All |
| 3047 | D3005_HM (#603) / Dragon Abyss 4th -  Door (#1680) | D3005 (#470) / Dragon Abyss Lv 5 SW - Landing (#1660) | Up | — | All |
| 3048 | D3005_JJ (#604) / Dragon Abyss 4th -  Door (#1683) | D3005 (#470) / Dragon Abyss Lv 5 SE - Landing (#1662) | Up | — | All |

### #2788 · D1505 (#81) / Row 3 Fake Bottom Doors (#1024) / D1505 (#81) / Row 2 Top Landing (#1022)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 3 Fake Bottom Doors (#1024) |
| DestinationRegion | D1505 (#81) / Row 2 Top Landing (#1022) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2789 · D1505 (#81) / Row 3 Fake Top Doors (#1023) / D1505 (#81) / Row 2 Bottom Landing (#1021)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 3 Fake Top Doors (#1023) |
| DestinationRegion | D1505 (#81) / Row 2 Bottom Landing (#1021) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2790 · D1505 (#81) / Row 3 Real Bottom door (#1075) / D1505 (#81) / Row 4 Top Landing (#1030)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 3 Real Bottom door (#1075) |
| DestinationRegion | D1505 (#81) / Row 4 Top Landing (#1030) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2791 · D1505 (#81) / Row 3 Real Top door (#1076) / D1505 (#81) / Row 4 Bottom Landing (#1029)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 3 Real Top door (#1076) |
| DestinationRegion | D1505 (#81) / Row 4 Bottom Landing (#1029) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2792 · D1505 (#81) / Row 4 Fake Bottom Doors (#1027) / D1505 (#81) / Row 3 Top Landing (#1026)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 4 Fake Bottom Doors (#1027) |
| DestinationRegion | D1505 (#81) / Row 3 Top Landing (#1026) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2793 · D1505 (#81) / Row 4 Fake Top Doors (#1028) / D1505 (#81) / Row 3 Bottom Landing (#1025)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 4 Fake Top Doors (#1028) |
| DestinationRegion | D1505 (#81) / Row 3 Bottom Landing (#1025) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2794 · D1505 (#81) / Row 4 Real Left Bottom door (#1077) / D1505 (#81) / Row 5 Left Top Landing (#1034)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 4 Real Left Bottom door (#1077) |
| DestinationRegion | D1505 (#81) / Row 5 Left Top Landing (#1034) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2795 · D1505 (#81) / Row 4 Real Left Top door (#1078) / D1505 (#81) / Row 5 Left Bottom Landing (#1033)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 4 Real Left Top door (#1078) |
| DestinationRegion | D1505 (#81) / Row 5 Left Bottom Landing (#1033) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2796 · D1505 (#81) / Row 4 Real Right Bottom door (#1099) / D1505 (#81) / Row 5 Right Top Landing (#1054)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 4 Real Right Bottom door (#1099) |
| DestinationRegion | D1505 (#81) / Row 5 Right Top Landing (#1054) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2797 · D1505 (#81) / Row 4 Real Right Top door (#1100) / D1505 (#81) / Row 5 Right Bottom Landing (#1053)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 4 Real Right Top door (#1100) |
| DestinationRegion | D1505 (#81) / Row 5 Right Bottom Landing (#1053) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2798 · D1505 (#81) / Row 5 Left Fake Bottom Doors (#1031) / D1505 (#81) / Row 4 Top Landing (#1030)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 5 Left Fake Bottom Doors (#1031) |
| DestinationRegion | D1505 (#81) / Row 4 Top Landing (#1030) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2799 · D1505 (#81) / Row 5 Left Fake Top Doors (#1032) / D1505 (#81) / Row 4 Bottom Landing (#1029)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 5 Left Fake Top Doors (#1032) |
| DestinationRegion | D1505 (#81) / Row 4 Bottom Landing (#1029) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2800 · D1505 (#81) / Row 5 Left Real Bottom door (#1079) / D1505 (#81) / Row 6 Left Top Landing (#1038)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 5 Left Real Bottom door (#1079) |
| DestinationRegion | D1505 (#81) / Row 6 Left Top Landing (#1038) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2801 · D1505 (#81) / Row 5 Left Real Top door (#1080) / D1505 (#81) / Row 6 Left Bottom Landing (#1037)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 5 Left Real Top door (#1080) |
| DestinationRegion | D1505 (#81) / Row 6 Left Bottom Landing (#1037) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2802 · D1505 (#81) / Row 5 Right Fake Bottom Doors (#1051) / D1505 (#81) / Row 4 Top Landing (#1030)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 5 Right Fake Bottom Doors (#1051) |
| DestinationRegion | D1505 (#81) / Row 4 Top Landing (#1030) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2803 · D1505 (#81) / Row 5 Right Fake Top Doors (#1052) / D1505 (#81) / Row 4 Bottom Landing (#1029)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 5 Right Fake Top Doors (#1052) |
| DestinationRegion | D1505 (#81) / Row 4 Bottom Landing (#1029) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2804 · D1505 (#81) / Row 5 Right Real Bottom door (#1089) / D1505 (#81) / Row 6 Right Top Landing (#1058)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 5 Right Real Bottom door (#1089) |
| DestinationRegion | D1505 (#81) / Row 6 Right Top Landing (#1058) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2805 · D1505 (#81) / Row 5 Right Real Top door (#1090) / D1505 (#81) / Row 6 Right Bottom Landing (#1057)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 5 Right Real Top door (#1090) |
| DestinationRegion | D1505 (#81) / Row 6 Right Bottom Landing (#1057) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2806 · D1505 (#81) / Row 6 Left Fake Bottom Doors (#1035) / D1505 (#81) / Row 5 Left Top Landing (#1034)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 6 Left Fake Bottom Doors (#1035) |
| DestinationRegion | D1505 (#81) / Row 5 Left Top Landing (#1034) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2807 · D1505 (#81) / Row 6 Left Fake Top Doors (#1036) / D1505 (#81) / Row 5 Left Bottom Landing (#1033)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 6 Left Fake Top Doors (#1036) |
| DestinationRegion | D1505 (#81) / Row 5 Left Bottom Landing (#1033) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2808 · D1505 (#81) / Row 6 Left Real Bottom door (#1081) / D1505 (#81) / Row 7 Left Top Landing (#1042)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 6 Left Real Bottom door (#1081) |
| DestinationRegion | D1505 (#81) / Row 7 Left Top Landing (#1042) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2809 · D1505 (#81) / Row 6 Left Real Top door (#1082) / D1505 (#81) / Row 7 Left Bottom Landing (#1041)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 6 Left Real Top door (#1082) |
| DestinationRegion | D1505 (#81) / Row 7 Left Bottom Landing (#1041) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2810 · D1505 (#81) / Row 6 Right Fake Bottom Doors (#1055) / D1505 (#81) / Row 5 Right Top Landing (#1054)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 6 Right Fake Bottom Doors (#1055) |
| DestinationRegion | D1505 (#81) / Row 5 Right Top Landing (#1054) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2811 · D1505 (#81) / Row 6 Right Fake Top Doors (#1056) / D1505 (#81) / Row 5 Right Bottom Landing (#1053)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 6 Right Fake Top Doors (#1056) |
| DestinationRegion | D1505 (#81) / Row 5 Right Bottom Landing (#1053) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2812 · D1505 (#81) / Row 6 Right Real Bottom door (#1091) / D1505 (#81) / Row 7 Right Top Landing (#1062)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 6 Right Real Bottom door (#1091) |
| DestinationRegion | D1505 (#81) / Row 7 Right Top Landing (#1062) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2813 · D1505 (#81) / Row 6 Right Real Top door (#1092) / D1505 (#81) / Row 7 Right Bottom Landing (#1061)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 6 Right Real Top door (#1092) |
| DestinationRegion | D1505 (#81) / Row 7 Right Bottom Landing (#1061) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2814 · D1505 (#81) / Row 7 Left Fake Bottom Doors (#1039) / D1505 (#81) / Row 6 Left Top Landing (#1038)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 7 Left Fake Bottom Doors (#1039) |
| DestinationRegion | D1505 (#81) / Row 6 Left Top Landing (#1038) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2815 · D1505 (#81) / Row 7 Left Fake Top Doors (#1040) / D1505 (#81) / Row 6 Left Bottom Landing (#1037)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 7 Left Fake Top Doors (#1040) |
| DestinationRegion | D1505 (#81) / Row 6 Left Bottom Landing (#1037) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2816 · D1505 (#81) / Row 7 Left Real Bottom door (#1083) / D1505 (#81) / Row 8 Left Top Landing (#1046)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 7 Left Real Bottom door (#1083) |
| DestinationRegion | D1505 (#81) / Row 8 Left Top Landing (#1046) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2817 · D1505 (#81) / Row 7 Left Real Top door (#1084) / D1505 (#81) / Row 8 Left Bottom Landing (#1045)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 7 Left Real Top door (#1084) |
| DestinationRegion | D1505 (#81) / Row 8 Left Bottom Landing (#1045) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2818 · D1505 (#81) / Row 7 Right Fake Bottom Doors (#1059) / D1505 (#81) / Row 6 Right Top Landing (#1058)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 7 Right Fake Bottom Doors (#1059) |
| DestinationRegion | D1505 (#81) / Row 6 Right Top Landing (#1058) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2819 · D1505 (#81) / Row 7 Right Fake Top Doors (#1060) / D1505 (#81) / Row 6 Right Bottom Landing (#1057)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 7 Right Fake Top Doors (#1060) |
| DestinationRegion | D1505 (#81) / Row 6 Right Bottom Landing (#1057) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2820 · D1505 (#81) / Row 7 Right Real Bottom door (#1093) / D1505 (#81) / Row 8 Right Top Landing (#1066)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 7 Right Real Bottom door (#1093) |
| DestinationRegion | D1505 (#81) / Row 8 Right Top Landing (#1066) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2821 · D1505 (#81) / Row 7 Right Real Top door (#1094) / D1505 (#81) / Row 8 Right Bottom Landing (#1065)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 7 Right Real Top door (#1094) |
| DestinationRegion | D1505 (#81) / Row 8 Right Bottom Landing (#1065) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2822 · D1505 (#81) / Row 8 Left Fake Bottom Doors (#1043) / D1505 (#81) / Row 7 Left Top Landing (#1042)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 8 Left Fake Bottom Doors (#1043) |
| DestinationRegion | D1505 (#81) / Row 7 Left Top Landing (#1042) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2823 · D1505 (#81) / Row 8 Left Fake Top Doors (#1044) / D1505 (#81) / Row 7 Left Bottom Landing (#1041)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 8 Left Fake Top Doors (#1044) |
| DestinationRegion | D1505 (#81) / Row 7 Left Bottom Landing (#1041) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2824 · D1505 (#81) / Row 8 Left Real Bottom door (#1085) / D1505 (#81) / Row 9 Left Top Landing (#1050)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 8 Left Real Bottom door (#1085) |
| DestinationRegion | D1505 (#81) / Row 9 Left Top Landing (#1050) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2825 · D1505 (#81) / Row 8 Left Real Top door (#1086) / D1505 (#81) / Row 9 Left Bottom Landing (#1049)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 8 Left Real Top door (#1086) |
| DestinationRegion | D1505 (#81) / Row 9 Left Bottom Landing (#1049) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2826 · D1505 (#81) / Row 8 Right Fake Bottom Doors (#1063) / D1505 (#81) / Row 7 Right Top Landing (#1062)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 8 Right Fake Bottom Doors (#1063) |
| DestinationRegion | D1505 (#81) / Row 7 Right Top Landing (#1062) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2827 · D1505 (#81) / Row 8 Right Fake Top Doors (#1064) / D1505 (#81) / Row 7 Right Bottom Landing (#1061)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 8 Right Fake Top Doors (#1064) |
| DestinationRegion | D1505 (#81) / Row 7 Right Bottom Landing (#1061) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2828 · D1505 (#81) / Row 8 Right Real Bottom door (#1095) / D1505 (#81) / Row 9 Right Top Landing (#1070)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 8 Right Real Bottom door (#1095) |
| DestinationRegion | D1505 (#81) / Row 9 Right Top Landing (#1070) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2829 · D1505 (#81) / Row 8 Right Real Top door (#1096) / D1505 (#81) / Row 9 Right Bottom Landing (#1069)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 8 Right Real Top door (#1096) |
| DestinationRegion | D1505 (#81) / Row 9 Right Bottom Landing (#1069) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2830 · D1505 (#81) / Row 9 Left Fake Bottom Doors (#1047) / D1505 (#81) / Row 8 Left Top Landing (#1046)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 9 Left Fake Bottom Doors (#1047) |
| DestinationRegion | D1505 (#81) / Row 8 Left Top Landing (#1046) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2831 · D1505 (#81) / Row 9 Left Fake Top Doors (#1048) / D1505 (#81) / Row 8 Left Bottom Landing (#1045)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 9 Left Fake Top Doors (#1048) |
| DestinationRegion | D1505 (#81) / Row 8 Left Bottom Landing (#1045) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2832 · D1505 (#81) / Row 9 Left Real Bottom door (#1087) / D1505 (#81) / Row 5 Right Top Landing (#1054)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 9 Left Real Bottom door (#1087) |
| DestinationRegion | D1505 (#81) / Row 5 Right Top Landing (#1054) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2833 · D1505 (#81) / Row 9 Left Real Top door (#1088) / D1505 (#81) / Row 5 Right Bottom Landing (#1053)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 9 Left Real Top door (#1088) |
| DestinationRegion | D1505 (#81) / Row 5 Right Bottom Landing (#1053) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2834 · D1505 (#81) / Row 9 Right Fake Bottom Doors (#1067) / D1505 (#81) / Row 8 Right Top Landing (#1066)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 9 Right Fake Bottom Doors (#1067) |
| DestinationRegion | D1505 (#81) / Row 8 Right Top Landing (#1066) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2835 · D1505 (#81) / Row 9 Right Fake Top Doors (#1068) / D1505 (#81) / Row 8 Right Bottom Landing (#1065)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 9 Right Fake Top Doors (#1068) |
| DestinationRegion | D1505 (#81) / Row 8 Right Bottom Landing (#1065) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2836 · D1505 (#81) / Row 9 Right Real Bottom door (#1097) / D1505 (#81) / Row 5 Left Top Landing (#1034)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 9 Right Real Bottom door (#1097) |
| DestinationRegion | D1505 (#81) / Row 5 Left Top Landing (#1034) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2837 · D1505 (#81) / Row 9 Right Real Top door (#1098) / D1505 (#81) / Row 5 Left Bottom Landing (#1033)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 9 Right Real Top door (#1098) |
| DestinationRegion | D1505 (#81) / Row 5 Left Bottom Landing (#1033) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2838 · D15032 (#77) / Top Door (#958) / D1502 (#75) / Floor 3 Top Landing (#944)

| 字段 | 值 |
|---|---|
| SourceRegion | D15032 (#77) / Top Door (#958) |
| DestinationRegion | D1502 (#75) / Floor 3 Top Landing (#944) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2839 · D15031 (#76) / Right Door (#953) / D1502 (#75) / Floor 3 Right Landing (#946)

| 字段 | 值 |
|---|---|
| SourceRegion | D15031 (#76) / Right Door (#953) |
| DestinationRegion | D1502 (#75) / Floor 3 Right Landing (#946) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2840 · D15034 (#79) / Bottom Door (#968) / D1502 (#75) / Floor 3 Bottom Landing (#948)

| 字段 | 值 |
|---|---|
| SourceRegion | D15034 (#79) / Bottom Door (#968) |
| DestinationRegion | D1502 (#75) / Floor 3 Bottom Landing (#948) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2841 · D15033 (#78) / Left Door (#963) / D1502 (#75) / Floor 3 Left Landing (#950)

| 字段 | 值 |
|---|---|
| SourceRegion | D15033 (#78) / Left Door (#963) |
| DestinationRegion | D1502 (#75) / Floor 3 Left Landing (#950) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2842 · 12 (#292) / Departed Valley Door (#1126) / D2501 (#298) / Entrance Landing (#1130)

| 字段 | 值 |
|---|---|
| SourceRegion | 12 (#292) / Departed Valley Door (#1126) |
| DestinationRegion | D2501 (#298) / Entrance Landing (#1130) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2843 · D2501 (#298) / Entrance Door (#1129) / 12 (#292) / Departed Valley Landing (#1127)

| 字段 | 值 |
|---|---|
| SourceRegion | D2501 (#298) / Entrance Door (#1129) |
| DestinationRegion | 12 (#292) / Departed Valley Landing (#1127) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2844 · D2501 (#298) / Floor 2 Door (#1131) / D2502 (#299) / Floor 1 Landing (#1135)

| 字段 | 值 |
|---|---|
| SourceRegion | D2501 (#298) / Floor 2 Door (#1131) |
| DestinationRegion | D2502 (#299) / Floor 1 Landing (#1135) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2845 · D2502 (#299) / Floor 1 Door (#1134) / D2501 (#298) / Floor 2 Landing (#1132)

| 字段 | 值 |
|---|---|
| SourceRegion | D2502 (#299) / Floor 1 Door (#1134) |
| DestinationRegion | D2501 (#298) / Floor 2 Landing (#1132) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2846 · D2502 (#299) / Floor 3 Door (#1136) / D2503 (#300) / Floor 2 Landing (#1140)

| 字段 | 值 |
|---|---|
| SourceRegion | D2502 (#299) / Floor 3 Door (#1136) |
| DestinationRegion | D2503 (#300) / Floor 2 Landing (#1140) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2847 · D2503 (#300) / Floor 2 Door (#1139) / D2502 (#299) / Floor 3 Landing (#1137)

| 字段 | 值 |
|---|---|
| SourceRegion | D2503 (#300) / Floor 2 Door (#1139) |
| DestinationRegion | D2502 (#299) / Floor 3 Landing (#1137) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2848 · D2503 (#300) / Banyo Island Door (#1141) / 13 (#293) / Departed Cave Landing (#1146)

| 字段 | 值 |
|---|---|
| SourceRegion | D2503 (#300) / Banyo Island Door (#1141) |
| DestinationRegion | 13 (#293) / Departed Cave Landing (#1146) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2849 · 13 (#293) / Departed Cave Door (#1145) / D2503 (#300) / Banyo Island Landing (#1142)

| 字段 | 值 |
|---|---|
| SourceRegion | 13 (#293) / Departed Cave Door (#1145) |
| DestinationRegion | D2503 (#300) / Banyo Island Landing (#1142) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2850 · 13 (#293) / Banyo Cave Door (#1147) / D2601 (#301) / Landing (#1151)

| 字段 | 值 |
|---|---|
| SourceRegion | 13 (#293) / Banyo Cave Door (#1147) |
| DestinationRegion | D2601 (#301) / Landing (#1151) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2851 · D2601 (#301) / Door (#1150) / 13 (#293) / Banyo Cave Landing (#1148)

| 字段 | 值 |
|---|---|
| SourceRegion | D2601 (#301) / Door (#1150) |
| DestinationRegion | 13 (#293) / Banyo Cave Landing (#1148) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2852 · D002 (#13) / Jinchon Door (#234) / D1200 (#42) / Desert Landing (#1196)

| 字段 | 值 |
|---|---|
| SourceRegion | D002 (#13) / Jinchon Door (#234) |
| DestinationRegion | D1200 (#42) / Desert Landing (#1196) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2853 · 5 (#9) / Jinchon Door (#206) / D1200 (#42) / Mud Wall Landing (#1198)

| 字段 | 值 |
|---|---|
| SourceRegion | 5 (#9) / Jinchon Door (#206) |
| DestinationRegion | D1200 (#42) / Mud Wall Landing (#1198) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2854 · D1200 (#42) / Desert Door (#1195) / D002 (#13) / Jinchon Landing (#235)

| 字段 | 值 |
|---|---|
| SourceRegion | D1200 (#42) / Desert Door (#1195) |
| DestinationRegion | D002 (#13) / Jinchon Landing (#235) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2855 · D1200 (#42) / Mud Wall Door (#1197) / 5 (#9) / Junchon Landing (#207)

| 字段 | 值 |
|---|---|
| SourceRegion | D1200 (#42) / Mud Wall Door (#1197) |
| DestinationRegion | 5 (#9) / Junchon Landing (#207) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2856 · D1200 (#42) / Floor 2 Door - N (#1199) / D12014 (#47) / Floor 1 Landing (#1222)

| 字段 | 值 |
|---|---|
| SourceRegion | D1200 (#42) / Floor 2 Door - N (#1199) |
| DestinationRegion | D12014 (#47) / Floor 1 Landing (#1222) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2857 · D1200 (#42) / Floor 2 Door - E (#1201) / D12013 (#46) / Floor 1 Landing (#1217)

| 字段 | 值 |
|---|---|
| SourceRegion | D1200 (#42) / Floor 2 Door - E (#1201) |
| DestinationRegion | D12013 (#46) / Floor 1 Landing (#1217) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2858 · D1200 (#42) / Floor 2 Door - S (#1203) / D12012 (#45) / Floor 1 Landing (#1214)

| 字段 | 值 |
|---|---|
| SourceRegion | D1200 (#42) / Floor 2 Door - S (#1203) |
| DestinationRegion | D12012 (#45) / Floor 1 Landing (#1214) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2859 · D1200 (#42) / Floor 2 Door - W (#1205) / D12011 (#43) / Floor 1 Landing (#1209)

| 字段 | 值 |
|---|---|
| SourceRegion | D1200 (#42) / Floor 2 Door - W (#1205) |
| DestinationRegion | D12011 (#43) / Floor 1 Landing (#1209) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2860 · D12011 (#43) / Floor 1 Door (#1208) / D1200 (#42) / Floor 2 Landing - W (#1206)

| 字段 | 值 |
|---|---|
| SourceRegion | D12011 (#43) / Floor 1 Door (#1208) |
| DestinationRegion | D1200 (#42) / Floor 2 Landing - W (#1206) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2861 · D12011 (#43) / Floor 3 Door (#1210) / D12021 (#48) / Floor 2 Landing (#1227)

| 字段 | 值 |
|---|---|
| SourceRegion | D12011 (#43) / Floor 3 Door (#1210) |
| DestinationRegion | D12021 (#48) / Floor 2 Landing (#1227) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2862 · D12012 (#45) / Floor 1 Door (#1213) / D1200 (#42) / Floor 2 Landing - S (#1204)

| 字段 | 值 |
|---|---|
| SourceRegion | D12012 (#45) / Floor 1 Door (#1213) |
| DestinationRegion | D1200 (#42) / Floor 2 Landing - S (#1204) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2863 · D12013 (#46) / Floor 1 Door (#1216) / D1200 (#42) / Floor 2 Landing - E (#1202)

| 字段 | 值 |
|---|---|
| SourceRegion | D12013 (#46) / Floor 1 Door (#1216) |
| DestinationRegion | D1200 (#42) / Floor 2 Landing - E (#1202) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2864 · D12013 (#46) / Floor 3 Door (#1218) / D12023 (#50) / Floor 2 Landing (#1237)

| 字段 | 值 |
|---|---|
| SourceRegion | D12013 (#46) / Floor 3 Door (#1218) |
| DestinationRegion | D12023 (#50) / Floor 2 Landing (#1237) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2865 · D12014 (#47) / Floor 1 Door (#1221) / D1200 (#42) / Floor 2 Landing - N (#1200)

| 字段 | 值 |
|---|---|
| SourceRegion | D12014 (#47) / Floor 1 Door (#1221) |
| DestinationRegion | D1200 (#42) / Floor 2 Landing - N (#1200) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2866 · D12014 (#47) / Floor 3 Door (#1223) / D12024 (#51) / Floor 2 Landing (#1242)

| 字段 | 值 |
|---|---|
| SourceRegion | D12014 (#47) / Floor 3 Door (#1223) |
| DestinationRegion | D12024 (#51) / Floor 2 Landing (#1242) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2867 · D12021 (#48) / Floor 2 Door (#1226) / D12011 (#43) / Floor 3 Landing (#1211)

| 字段 | 值 |
|---|---|
| SourceRegion | D12021 (#48) / Floor 2 Door (#1226) |
| DestinationRegion | D12011 (#43) / Floor 3 Landing (#1211) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2868 · D12021 (#48) / Floor 3 Door - S (#1228) / D12022 (#49) / Floor 3 Landing - W (#1232)

| 字段 | 值 |
|---|---|
| SourceRegion | D12021 (#48) / Floor 3 Door - S (#1228) |
| DestinationRegion | D12022 (#49) / Floor 3 Landing - W (#1232) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2869 · D12022 (#49) / Floor 3 Door - W (#1231) / D12021 (#48) / Floor 3 Landing - S (#1229)

| 字段 | 值 |
|---|---|
| SourceRegion | D12022 (#49) / Floor 3 Door - W (#1231) |
| DestinationRegion | D12021 (#48) / Floor 3 Landing - S (#1229) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2870 · D12022 (#49) / Floor 4 Door - S (#1233) / D12031 (#52) / Floor 3 Landing (#1250)

| 字段 | 值 |
|---|---|
| SourceRegion | D12022 (#49) / Floor 4 Door - S (#1233) |
| DestinationRegion | D12031 (#52) / Floor 3 Landing (#1250) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2871 · D12023 (#50) / Floor 2 Door (#1236) / D12013 (#46) / Floor 3 Landing (#1219)

| 字段 | 值 |
|---|---|
| SourceRegion | D12023 (#50) / Floor 2 Door (#1236) |
| DestinationRegion | D12013 (#46) / Floor 3 Landing (#1219) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2872 · D12023 (#50) / Floor 4 Door (#1238) / D12032 (#53) / Floor 3 Landing (#1255)

| 字段 | 值 |
|---|---|
| SourceRegion | D12023 (#50) / Floor 4 Door (#1238) |
| DestinationRegion | D12032 (#53) / Floor 3 Landing (#1255) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2873 · D12024 (#51) / Floor 2 Door (#1241) / D12014 (#47) / Floor 3 Landing (#1224)

| 字段 | 值 |
|---|---|
| SourceRegion | D12024 (#51) / Floor 2 Door (#1241) |
| DestinationRegion | D12014 (#47) / Floor 3 Landing (#1224) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2874 · D12024 (#51) / Floor 4 Door (#1243) / D12033 (#54) / Floor 3 Landing (#1247)

| 字段 | 值 |
|---|---|
| SourceRegion | D12024 (#51) / Floor 4 Door (#1243) |
| DestinationRegion | D12033 (#54) / Floor 3 Landing (#1247) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2875 · D12031 (#52) / Floor 3 Door (#1249) / D12022 (#49) / Floor 4 Landing - S (#1234)

| 字段 | 值 |
|---|---|
| SourceRegion | D12031 (#52) / Floor 3 Door (#1249) |
| DestinationRegion | D12022 (#49) / Floor 4 Landing - S (#1234) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2876 · D12031 (#52) / Floor 4 Door - E (#1251) / D12032 (#53) / Floor 4 Landing - S (#1257)

| 字段 | 值 |
|---|---|
| SourceRegion | D12031 (#52) / Floor 4 Door - E (#1251) |
| DestinationRegion | D12032 (#53) / Floor 4 Landing - S (#1257) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2877 · D12032 (#53) / Floor 3 Door (#1254) / D12023 (#50) / Floor 4 Landing (#1239)

| 字段 | 值 |
|---|---|
| SourceRegion | D12032 (#53) / Floor 3 Door (#1254) |
| DestinationRegion | D12023 (#50) / Floor 4 Landing (#1239) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2878 · D12032 (#53) / Floor 4 Door - S (#1256) / D12031 (#52) / Floor 4 Landing - E (#1252)

| 字段 | 值 |
|---|---|
| SourceRegion | D12032 (#53) / Floor 4 Door - S (#1256) |
| DestinationRegion | D12031 (#52) / Floor 4 Landing - E (#1252) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2879 · D12032 (#53) / Floor 5 Door (#1258) / D12041 (#55) / Floor 4 Landing (#1262)

| 字段 | 值 |
|---|---|
| SourceRegion | D12032 (#53) / Floor 5 Door (#1258) |
| DestinationRegion | D12041 (#55) / Floor 4 Landing (#1262) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2880 · D12033 (#54) / Floor 3 Door (#1246) / D12024 (#51) / Floor 4 Landing (#1244)

| 字段 | 值 |
|---|---|
| SourceRegion | D12033 (#54) / Floor 3 Door (#1246) |
| DestinationRegion | D12024 (#51) / Floor 4 Landing (#1244) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2881 · D12041 (#55) / Floor 4 Door (#1261) / D12032 (#53) / Floor 5 Landing (#1259)

| 字段 | 值 |
|---|---|
| SourceRegion | D12041 (#55) / Floor 4 Door (#1261) |
| DestinationRegion | D12032 (#53) / Floor 5 Landing (#1259) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2882 · D12041 (#55) / Floor 5 Door - N (#1263) / D12042 (#56) / Floor 5 Landing - E (#1267)

| 字段 | 值 |
|---|---|
| SourceRegion | D12041 (#55) / Floor 5 Door - N (#1263) |
| DestinationRegion | D12042 (#56) / Floor 5 Landing - E (#1267) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2883 · D12042 (#56) / Floor 5 Door - E (#1266) / D12041 (#55) / Floor 5 Landing - N (#1264)

| 字段 | 值 |
|---|---|
| SourceRegion | D12042 (#56) / Floor 5 Door - E (#1266) |
| DestinationRegion | D12041 (#55) / Floor 5 Landing - N (#1264) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2884 · D12042 (#56) / Floor 6 Door (#1268) / D1205 (#57) / Floor 5 Landing (#1272)

| 字段 | 值 |
|---|---|
| SourceRegion | D12042 (#56) / Floor 6 Door (#1268) |
| DestinationRegion | D1205 (#57) / Floor 5 Landing (#1272) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2885 · D1205 (#57) / Floor 5 Door (#1271) / D12042 (#56) / Floor 6 Landing (#1269)

| 字段 | 值 |
|---|---|
| SourceRegion | D1205 (#57) / Floor 5 Door (#1271) |
| DestinationRegion | D12042 (#56) / Floor 6 Landing (#1269) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2888 · D1205 (#57) / Floor 7 Door (#1273) / D1206 (#58) / Landing (#1277)

| 字段 | 值 |
|---|---|
| SourceRegion | D1205 (#57) / Floor 7 Door (#1273) |
| DestinationRegion | D1206 (#58) / Landing (#1277) |
| Icon | Down |
| NeedSpawn | Jinchon Devil (#199) / D1206 (#58) / Boss Area (#1278) (#5332) |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2889 · D002 (#13) / Black Palace Door (#240) / D1301 (#62) / Desert Landing (#1161)

| 字段 | 值 |
|---|---|
| SourceRegion | D002 (#13) / Black Palace Door (#240) |
| DestinationRegion | D1301 (#62) / Desert Landing (#1161) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2890 · 5 (#9) / Black Palace Door (#208) / D1301 (#62) / Mud Wall Landing (#1163)

| 字段 | 值 |
|---|---|
| SourceRegion | 5 (#9) / Black Palace Door (#208) |
| DestinationRegion | D1301 (#62) / Mud Wall Landing (#1163) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2891 · D1301 (#62) / Desert Door (#1160) / D002 (#13) / Black Palance Landing (#241)

| 字段 | 值 |
|---|---|
| SourceRegion | D1301 (#62) / Desert Door (#1160) |
| DestinationRegion | D002 (#13) / Black Palance Landing (#241) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2892 · D1301 (#62) / Mud Wall Door (#1162) / 5 (#9) / Black Palace Landing (#209)

| 字段 | 值 |
|---|---|
| SourceRegion | D1301 (#62) / Mud Wall Door (#1162) |
| DestinationRegion | 5 (#9) / Black Palace Landing (#209) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2893 · D1301 (#62) / Floor 2 Door W (#1164) / D13021 (#63) / Floor 1 Landing (#1170)

| 字段 | 值 |
|---|---|
| SourceRegion | D1301 (#62) / Floor 2 Door W (#1164) |
| DestinationRegion | D13021 (#63) / Floor 1 Landing (#1170) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2894 · D1301 (#62) / Floor 2 Door E (#1166) / D13022 (#64) / Floor 1 Landing (#1175)

| 字段 | 值 |
|---|---|
| SourceRegion | D1301 (#62) / Floor 2 Door E (#1166) |
| DestinationRegion | D13022 (#64) / Floor 1 Landing (#1175) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2895 · D13021 (#63) / Floor 1 Door (#1169) / D1301 (#62) / Floor 2 Landing W (#1165)

| 字段 | 值 |
|---|---|
| SourceRegion | D13021 (#63) / Floor 1 Door (#1169) |
| DestinationRegion | D1301 (#62) / Floor 2 Landing W (#1165) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2896 · D13021 (#63) / Floor 3 Door (#1171) / D1303 (#65) / Floor 2 Landing - W (#1180)

| 字段 | 值 |
|---|---|
| SourceRegion | D13021 (#63) / Floor 3 Door (#1171) |
| DestinationRegion | D1303 (#65) / Floor 2 Landing - W (#1180) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2897 · D13022 (#64) / Floor 1 Door (#1174) / D1301 (#62) / Floor 2 Landing E (#1167)

| 字段 | 值 |
|---|---|
| SourceRegion | D13022 (#64) / Floor 1 Door (#1174) |
| DestinationRegion | D1301 (#62) / Floor 2 Landing E (#1167) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2898 · D13022 (#64) / Floor 3 Door (#1176) / D1303 (#65) / Floor 2 Landing - E (#1182)

| 字段 | 值 |
|---|---|
| SourceRegion | D13022 (#64) / Floor 3 Door (#1176) |
| DestinationRegion | D1303 (#65) / Floor 2 Landing - E (#1182) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2899 · D1303 (#65) / Floor 2 Door - W (#1179) / D13021 (#63) / Floor 3 Landing (#1172)

| 字段 | 值 |
|---|---|
| SourceRegion | D1303 (#65) / Floor 2 Door - W (#1179) |
| DestinationRegion | D13021 (#63) / Floor 3 Landing (#1172) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2900 · D1303 (#65) / Floor 2 Door - E (#1181) / D13022 (#64) / Floor 3 Landing (#1177)

| 字段 | 值 |
|---|---|
| SourceRegion | D1303 (#65) / Floor 2 Door - E (#1181) |
| DestinationRegion | D13022 (#64) / Floor 3 Landing (#1177) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2901 · D1303 (#65) / Floor 4 Door (#1183) / D1304 (#66) / Floor 3 Landing (#1187)

| 字段 | 值 |
|---|---|
| SourceRegion | D1303 (#65) / Floor 4 Door (#1183) |
| DestinationRegion | D1304 (#66) / Floor 3 Landing (#1187) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2902 · D1304 (#66) / Floor 5 Door (#1188) / D1305 (#67) / Landing (#1192)

| 字段 | 值 |
|---|---|
| SourceRegion | D1304 (#66) / Floor 5 Door (#1188) |
| DestinationRegion | D1305 (#67) / Landing (#1192) |
| Icon | Up |
| NeedSpawn | Black Palace Demon (#200) / D1305 (#67) / Boss Area (#1193) (#5325) |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2904 · D112 (#40) / Floor 1 Door (#529) / D111 (#39) / Floor 2 Landing (#523)

| 字段 | 值 |
|---|---|
| SourceRegion | D112 (#40) / Floor 1 Door (#529) |
| DestinationRegion | D111 (#39) / Floor 2 Landing (#523) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2905 · 14_000 (#459) / Door (#1279) / 0 (#1) / Assassin's Hideout Landing (#1280)

| 字段 | 值 |
|---|---|
| SourceRegion | 14_000 (#459) / Door (#1279) |
| DestinationRegion | 0 (#1) / Assassin's Hideout Landing (#1280) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2906 · D1104 (#36) / Traps (#1158) / D1104 (#36) / Whole Map (#407)

| 字段 | 值 |
|---|---|
| SourceRegion | D1104 (#36) / Traps (#1158) |
| DestinationRegion | D1104 (#36) / Whole Map (#407) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2907 · D1105 (#37) / Traps (#1157) / D1105 (#37) / Whole Map (#413)

| 字段 | 值 |
|---|---|
| SourceRegion | D1105 (#37) / Traps (#1157) |
| DestinationRegion | D1105 (#37) / Whole Map (#413) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2908 · D1402 (#69) / Traps (#1153) / D1401 (#68) / Teleport Area (#711)

| 字段 | 值 |
|---|---|
| SourceRegion | D1402 (#69) / Traps (#1153) |
| DestinationRegion | D1401 (#68) / Teleport Area (#711) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2909 · D1403 (#70) / Traps (#1154) / D1401 (#68) / Teleport Area (#711)

| 字段 | 值 |
|---|---|
| SourceRegion | D1403 (#70) / Traps (#1154) |
| DestinationRegion | D1401 (#68) / Teleport Area (#711) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2910 · D1404 (#71) / Traps (#1155) / D1401 (#68) / Teleport Area (#711)

| 字段 | 值 |
|---|---|
| SourceRegion | D1404 (#71) / Traps (#1155) |
| DestinationRegion | D1401 (#68) / Teleport Area (#711) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2911 · D1405 (#72) / Traps (#1156) / D1401 (#68) / Teleport Area (#711)

| 字段 | 值 |
|---|---|
| SourceRegion | D1405 (#72) / Traps (#1156) |
| DestinationRegion | D1401 (#68) / Teleport Area (#711) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2912 · D301 (#139) / Floor 2 Door (#648) / D302 (#140) / Floor 1 Landing (#655)

| 字段 | 值 |
|---|---|
| SourceRegion | D301 (#139) / Floor 2 Door (#648) |
| DestinationRegion | D302 (#140) / Floor 1 Landing (#655) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2913 · D302 (#140) / Floor 1 Door (#654) / D301 (#139) / Floor 2 Landing (#649)

| 字段 | 值 |
|---|---|
| SourceRegion | D302 (#140) / Floor 1 Door (#654) |
| DestinationRegion | D301 (#139) / Floor 2 Landing (#649) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2914 · D302 (#140) / Floor 3 Door (#656) / D303 (#141) / Floor 2 Landing (#662)

| 字段 | 值 |
|---|---|
| SourceRegion | D302 (#140) / Floor 3 Door (#656) |
| DestinationRegion | D303 (#141) / Floor 2 Landing (#662) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2915 · D303 (#141) / Floor 2 Door (#661) / D302 (#140) / Floor 3 Landing (#657)

| 字段 | 值 |
|---|---|
| SourceRegion | D303 (#141) / Floor 2 Door (#661) |
| DestinationRegion | D302 (#140) / Floor 3 Landing (#657) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2916 · 0_001 (#3) / Door (#40) / 0_000 (#2) / Left Landing (#37)

| 字段 | 值 |
|---|---|
| SourceRegion | 0_001 (#3) / Door (#40) |
| DestinationRegion | 0_000 (#2) / Left Landing (#37) |
| Icon | Building |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2917 · 0_002 (#4) / Door (#42) / 0_000 (#2) / Right Landing (#39)

| 字段 | 值 |
|---|---|
| SourceRegion | 0_002 (#4) / Door (#42) |
| DestinationRegion | 0_000 (#2) / Right Landing (#39) |
| Icon | Building |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2918 · D1405 (#72) / Exit Door (#738) / D1401 (#68) / Teleport Area (#711)

| 字段 | 值 |
|---|---|
| SourceRegion | D1405 (#72) / Exit Door (#738) |
| DestinationRegion | D1401 (#68) / Teleport Area (#711) |
| Icon | Exit |
| NeedHole | false |
| Effect | SpecialRepair |
| RequiredClass | All |
| SkipValidation | false |

### #2919 · 4 (#8) / Southern Dunes - Door (#186) / D4000 (#587) / Numa Village - Landing (#1324)

| 字段 | 值 |
|---|---|
| SourceRegion | 4 (#8) / Southern Dunes - Door (#186) |
| DestinationRegion | D4000 (#587) / Numa Village - Landing (#1324) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2920 · D4000 (#587) / Numa Village - Door (#1323) / 4 (#8) / Southern Dunes - Landing (#187)

| 字段 | 值 |
|---|---|
| SourceRegion | D4000 (#587) / Numa Village - Door (#1323) |
| DestinationRegion | 4 (#8) / Southern Dunes - Landing (#187) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2921 · D4000 (#587) / Southern Wastes - Door (#1325) / D4001 (#588) / Southern Dunes - Landing (#1329)

| 字段 | 值 |
|---|---|
| SourceRegion | D4000 (#587) / Southern Wastes - Door (#1325) |
| DestinationRegion | D4001 (#588) / Southern Dunes - Landing (#1329) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2922 · D4001 (#588) / Southern Dunes - Door (#1328) / D4000 (#587) / Southern Wastes - Landing (#1326)

| 字段 | 值 |
|---|---|
| SourceRegion | D4001 (#588) / Southern Dunes - Door (#1328) |
| DestinationRegion | D4000 (#587) / Southern Wastes - Landing (#1326) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2923 · D4001 (#588) / Southern Coast - Door (#1330) / D4002 (#589) / Southern Wastes - Landing (#1334)

| 字段 | 值 |
|---|---|
| SourceRegion | D4001 (#588) / Southern Coast - Door (#1330) |
| DestinationRegion | D4002 (#589) / Southern Wastes - Landing (#1334) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2924 · D4002 (#589) / Southern Wastes - Door (#1333) / D4001 (#588) / Southern Coast - Landing (#1331)

| 字段 | 值 |
|---|---|
| SourceRegion | D4002 (#589) / Southern Wastes - Door (#1333) |
| DestinationRegion | D4001 (#588) / Southern Coast - Landing (#1331) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2925 · D4002 (#589) / Southern Check Point - Door (#1335) / D4003 (#590) / Southern Coast - Landing (#1441)

| 字段 | 值 |
|---|---|
| SourceRegion | D4002 (#589) / Southern Check Point - Door (#1335) |
| DestinationRegion | D4003 (#590) / Southern Coast - Landing (#1441) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2926 · D4003 (#590) / Beyond Shore - Door (#1340) / 16_001 (#568) / Southern Check Point - Landing (#1343)

| 字段 | 值 |
|---|---|
| SourceRegion | D4003 (#590) / Beyond Shore - Door (#1340) |
| DestinationRegion | 16_001 (#568) / Southern Check Point - Landing (#1343) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2927 · D4003 (#590) / Southern Coast - Door (#1440) / D4002 (#589) / Southern Check Point - Landing (#1336)

| 字段 | 值 |
|---|---|
| SourceRegion | D4003 (#590) / Southern Coast - Door (#1440) |
| DestinationRegion | D4002 (#589) / Southern Check Point - Landing (#1336) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2928 · 16_001 (#568) / Southern Check Point - Door (#1342) / D4003 (#590) / Beyond Shore - Landing (#1341)

| 字段 | 值 |
|---|---|
| SourceRegion | 16_001 (#568) / Southern Check Point - Door (#1342) |
| DestinationRegion | D4003 (#590) / Beyond Shore - Landing (#1341) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2929 · 16_001 (#568) / Southern Wall - Door (#1407) / D4101 (#591) / Beyond Shore - Landing (#1345)

| 字段 | 值 |
|---|---|
| SourceRegion | 16_001 (#568) / Southern Wall - Door (#1407) |
| DestinationRegion | D4101 (#591) / Beyond Shore - Landing (#1345) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2930 · 16_001 (#568) / Western Coast - Door (#1442) / 16_002 (#569) / Beyond Shore - Landing (#1348)

| 字段 | 值 |
|---|---|
| SourceRegion | 16_001 (#568) / Western Coast - Door (#1442) |
| DestinationRegion | 16_002 (#569) / Beyond Shore - Landing (#1348) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2931 · 16_002 (#569) / Beyond Shore - Door (#1347) / 16_001 (#568) / Western Coast - Landing (#1443)

| 字段 | 值 |
|---|---|
| SourceRegion | 16_002 (#569) / Beyond Shore - Door (#1347) |
| DestinationRegion | 16_001 (#568) / Western Coast - Landing (#1443) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2932 · 16_002 (#569) / Western Pass - Door (#1349) / 16_003 (#570) / Western Coast - Landing (#1353)

| 字段 | 值 |
|---|---|
| SourceRegion | 16_002 (#569) / Western Pass - Door (#1349) |
| DestinationRegion | 16_003 (#570) / Western Coast - Landing (#1353) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2933 · 16_003 (#570) / Western Coast - Door (#1352) / 16_002 (#569) / Western Pass - Landing (#1350)

| 字段 | 值 |
|---|---|
| SourceRegion | 16_003 (#570) / Western Coast - Door (#1352) |
| DestinationRegion | 16_002 (#569) / Western Pass - Landing (#1350) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2934 · 16_003 (#570) / Western Arids - Door (#1364) / 16 (#567) / Western Pass - Landing (#1355)

| 字段 | 值 |
|---|---|
| SourceRegion | 16_003 (#570) / Western Arids - Door (#1364) |
| DestinationRegion | 16 (#567) / Western Pass - Landing (#1355) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2935 · 16 (#567) / Lost Oasis - Door 1 (#1356) / 17 (#571) / Western Arids - Landing 1 (#1368)

| 字段 | 值 |
|---|---|
| SourceRegion | 16 (#567) / Lost Oasis - Door 1 (#1356) |
| DestinationRegion | 17 (#571) / Western Arids - Landing 1 (#1368) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2936 · 16 (#567) / Lost Oasis - Door 2 (#1358) / 17 (#571) / Western Arids - Landing 2 (#1370)

| 字段 | 值 |
|---|---|
| SourceRegion | 16 (#567) / Lost Oasis - Door 2 (#1358) |
| DestinationRegion | 17 (#571) / Western Arids - Landing 2 (#1370) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2937 · 16 (#567) / Lost Oasis - Door 3 (#1360) / 17 (#571) / Western Arids - Landing 3 (#1372)

| 字段 | 值 |
|---|---|
| SourceRegion | 16 (#567) / Lost Oasis - Door 3 (#1360) |
| DestinationRegion | 17 (#571) / Western Arids - Landing 3 (#1372) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2938 · 16 (#567) / Lost Oasis - Door 4 (#1362) / 17 (#571) / Western Arids - Landing 4 (#1374)

| 字段 | 值 |
|---|---|
| SourceRegion | 16 (#567) / Lost Oasis - Door 4 (#1362) |
| DestinationRegion | 17 (#571) / Western Arids - Landing 4 (#1374) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2939 · 17 (#571) / Western Arids - Door 1 (#1367) / 16 (#567) / Lost Oasis - Landing 1 (#1357)

| 字段 | 值 |
|---|---|
| SourceRegion | 17 (#571) / Western Arids - Door 1 (#1367) |
| DestinationRegion | 16 (#567) / Lost Oasis - Landing 1 (#1357) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2940 · 17 (#571) / Western Arids - Door 2 (#1369) / 16 (#567) / Lost Oasis - Landing 2 (#1359)

| 字段 | 值 |
|---|---|
| SourceRegion | 17 (#571) / Western Arids - Door 2 (#1369) |
| DestinationRegion | 16 (#567) / Lost Oasis - Landing 2 (#1359) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2941 · 17 (#571) / Western Arids - Door 3 (#1371) / 16 (#567) / Lost Oasis - Landing 3 (#1361)

| 字段 | 值 |
|---|---|
| SourceRegion | 17 (#571) / Western Arids - Door 3 (#1371) |
| DestinationRegion | 16 (#567) / Lost Oasis - Landing 3 (#1361) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2942 · 17 (#571) / Western Arids - Door 4 (#1373) / 16 (#567) / Lost Oasis - Landing 4 (#1363)

| 字段 | 值 |
|---|---|
| SourceRegion | 17 (#571) / Western Arids - Door 4 (#1373) |
| DestinationRegion | 16 (#567) / Lost Oasis - Landing 4 (#1363) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2943 · 17 (#571) / Arid Flats - Door (#1375) / 18 (#572) / Lost Oasis - Landing (#1379)

| 字段 | 值 |
|---|---|
| SourceRegion | 17 (#571) / Arid Flats - Door (#1375) |
| DestinationRegion | 18 (#572) / Lost Oasis - Landing (#1379) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2944 · 18 (#572) / Lost Oasis - Door (#1378) / 17 (#571) / Aird Flats - Landing (#1376)

| 字段 | 值 |
|---|---|
| SourceRegion | 18 (#572) / Lost Oasis - Door (#1378) |
| DestinationRegion | 17 (#571) / Aird Flats - Landing (#1376) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2945 · 18 (#572) / Quartz Mine - Door (#1380) / ID7_000 (#593) / Arid Flats - Landing (#1384)

| 字段 | 值 |
|---|---|
| SourceRegion | 18 (#572) / Quartz Mine - Door (#1380) |
| DestinationRegion | ID7_000 (#593) / Arid Flats - Landing (#1384) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2946 · ID7_000 (#593) / Arid Flats - Door (#1383) / 18 (#572) / Quartz Mine - Landing (#1381)

| 字段 | 值 |
|---|---|
| SourceRegion | ID7_000 (#593) / Arid Flats - Door (#1383) |
| DestinationRegion | 18 (#572) / Quartz Mine - Landing (#1381) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2947 · ID7_000 (#593) / Quartz Mine Lv 2 - Door (#1386) / ID7_001 (#594) / Quartz Mine Lv 1 - Landing (#1389)

| 字段 | 值 |
|---|---|
| SourceRegion | ID7_000 (#593) / Quartz Mine Lv 2 - Door (#1386) |
| DestinationRegion | ID7_001 (#594) / Quartz Mine Lv 1 - Landing (#1389) |
| Icon | Down |
| NeedItem | Pure Quartz (#827) |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2948 · ID7_001 (#594) / Quartz Mine Lv 1 - Door (#1388) / ID7_000 (#593) / Quartz Mine Lv 2 - Landing (#1387)

| 字段 | 值 |
|---|---|
| SourceRegion | ID7_001 (#594) / Quartz Mine Lv 1 - Door (#1388) |
| DestinationRegion | ID7_000 (#593) / Quartz Mine Lv 2 - Landing (#1387) |
| Icon | Up |
| NeedItem | Pure Quartz (#827) |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2949 · ID7_001 (#594) / Quartz Mine Lv 3 - Door (#1390) / ID7_002 (#595) / Quartz Mine Lv 2 - Landing (#1394)

| 字段 | 值 |
|---|---|
| SourceRegion | ID7_001 (#594) / Quartz Mine Lv 3 - Door (#1390) |
| DestinationRegion | ID7_002 (#595) / Quartz Mine Lv 2 - Landing (#1394) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2950 · ID7_002 (#595) / Quartz Mine Lv 2 - Door (#1393) / ID7_001 (#594) / Quartz Mine Lv 3 - Landing (#1391)

| 字段 | 值 |
|---|---|
| SourceRegion | ID7_002 (#595) / Quartz Mine Lv 2 - Door (#1393) |
| DestinationRegion | ID7_001 (#594) / Quartz Mine Lv 3 - Landing (#1391) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2951 · ID7_002 (#595) / Quartz Mine Lv 4 - Door (#1395) / ID7_003 (#596) / Quartz Mine Lv 3 - Landing (#1399)

| 字段 | 值 |
|---|---|
| SourceRegion | ID7_002 (#595) / Quartz Mine Lv 4 - Door (#1395) |
| DestinationRegion | ID7_003 (#596) / Quartz Mine Lv 3 - Landing (#1399) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2952 · ID7_003 (#596) / Quartz Mine Lv 3 - Door (#1398) / ID7_002 (#595) / Quartz Mine Lv 4 - Landing (#1396)

| 字段 | 值 |
|---|---|
| SourceRegion | ID7_003 (#596) / Quartz Mine Lv 3 - Door (#1398) |
| DestinationRegion | ID7_002 (#595) / Quartz Mine Lv 4 - Landing (#1396) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2953 · ID7_003 (#596) / Quartz Mine Lv 5 - Door (#1400) / ID7_004 (#597) / Quartz Mine Lv 4 - Landing (#1404)

| 字段 | 值 |
|---|---|
| SourceRegion | ID7_003 (#596) / Quartz Mine Lv 5 - Door (#1400) |
| DestinationRegion | ID7_004 (#597) / Quartz Mine Lv 4 - Landing (#1404) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2955 · D4101 (#591) / Beyond Shore - Door (#1344) / 16_001 (#568) / Southern Wall - Landing (#1408)

| 字段 | 值 |
|---|---|
| SourceRegion | D4101 (#591) / Beyond Shore - Door (#1344) |
| DestinationRegion | 16_001 (#568) / Southern Wall - Landing (#1408) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2956 · D4101 (#591) / Lost Way - Door (#1444) / D4102 (#592) / Southern Wall - Landing (#1412)

| 字段 | 值 |
|---|---|
| SourceRegion | D4101 (#591) / Lost Way - Door (#1444) |
| DestinationRegion | D4102 (#592) / Southern Wall - Landing (#1412) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2957 · D4102 (#592) / Southern Wall - Door (#1411) / D4101 (#591) / Lost Way - Landing (#1445)

| 字段 | 值 |
|---|---|
| SourceRegion | D4102 (#592) / Southern Wall - Door (#1411) |
| DestinationRegion | D4101 (#591) / Lost Way - Landing (#1445) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2958 · D4102 (#592) / Lost Village - Door (#1446) / 19 (#573) / Lost Way - Landing (#1417)

| 字段 | 值 |
|---|---|
| SourceRegion | D4102 (#592) / Lost Village - Door (#1446) |
| DestinationRegion | 19 (#573) / Lost Way - Landing (#1417) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2959 · 19 (#573) / Lost Way - Door (#1416) / D4102 (#592) / Lost Village - Landing (#1447)

| 字段 | 值 |
|---|---|
| SourceRegion | 19 (#573) / Lost Way - Door (#1416) |
| DestinationRegion | D4102 (#592) / Lost Village - Landing (#1447) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2960 · 19 (#573) / Lost Pass - Door (#1419) / 19_1 (#574) / Lost Village - Landing (#1422)

| 字段 | 值 |
|---|---|
| SourceRegion | 19 (#573) / Lost Pass - Door (#1419) |
| DestinationRegion | 19_1 (#574) / Lost Village - Landing (#1422) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2961 · 19_1 (#574) / Lost Village - Door (#1421) / 19 (#573) / Lost Pass - Landing (#1420)

| 字段 | 值 |
|---|---|
| SourceRegion | 19_1 (#574) / Lost Village - Door (#1421) |
| DestinationRegion | 19 (#573) / Lost Pass - Landing (#1420) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2962 · 19_1 (#574) / Abandoned Town - Door (#1423) / ID9_00 (#598) / Lost Pass - Landing (#1426)

| 字段 | 值 |
|---|---|
| SourceRegion | 19_1 (#574) / Abandoned Town - Door (#1423) |
| DestinationRegion | ID9_00 (#598) / Lost Pass - Landing (#1426) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2963 · ID9_00 (#598) / Lost Pass - Door (#1425) / 19_1 (#574) / Abandoned Town - Landing (#1424)

| 字段 | 值 |
|---|---|
| SourceRegion | ID9_00 (#598) / Lost Pass - Door (#1425) |
| DestinationRegion | 19_1 (#574) / Abandoned Town - Landing (#1424) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2964 · ID9_00 (#598) / Forgotton Monastery - Door (#1427) / ID9_01 (#599) / Abandoned Town - Landing (#1431)

| 字段 | 值 |
|---|---|
| SourceRegion | ID9_00 (#598) / Forgotton Monastery - Door (#1427) |
| DestinationRegion | ID9_01 (#599) / Abandoned Town - Landing (#1431) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2965 · ID9_01 (#599) / Abandoned Town - Door (#1430) / ID9_00 (#598) / Forgotton Monastery - Landing (#1428)

| 字段 | 值 |
|---|---|
| SourceRegion | ID9_01 (#599) / Abandoned Town - Door (#1430) |
| DestinationRegion | ID9_00 (#598) / Forgotton Monastery - Landing (#1428) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2966 · ID9_01 (#599) / Forgotton Monastery Lv 2 - Door (#1432) / ID9_02 (#600) / Forgotton Monastery Lv 1 - Landing (#1436)

| 字段 | 值 |
|---|---|
| SourceRegion | ID9_01 (#599) / Forgotton Monastery Lv 2 - Door (#1432) |
| DestinationRegion | ID9_02 (#600) / Forgotton Monastery Lv 1 - Landing (#1436) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2967 · ID9_02 (#600) / Forgotton Monastery Lv 1 - Door (#1435) / ID9_01 (#599) / Forgotton Monastery Lv 2 - Landing (#1433)

| 字段 | 值 |
|---|---|
| SourceRegion | ID9_02 (#600) / Forgotton Monastery Lv 1 - Door (#1435) |
| DestinationRegion | ID9_01 (#599) / Forgotton Monastery Lv 2 - Landing (#1433) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2968 · 16 (#567) / Western Pass - Door (#1354) / 16_003 (#570) / Western Arids - Landing (#1365)

| 字段 | 值 |
|---|---|
| SourceRegion | 16 (#567) / Western Pass - Door (#1354) |
| DestinationRegion | 16_003 (#570) / Western Arids - Landing (#1365) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2969 · 1 (#5) / Unknown Province Door (#49) / 11 (#291) / Lost Paradise - Landing (#1450)

| 字段 | 值 |
|---|---|
| SourceRegion | 1 (#5) / Unknown Province Door (#49) |
| DestinationRegion | 11 (#291) / Lost Paradise - Landing (#1450) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2970 · 11 (#291) / Lost Paradise - Door (#1449) / 1 (#5) / Unknown Province Landing (#50)

| 字段 | 值 |
|---|---|
| SourceRegion | 11 (#291) / Lost Paradise - Door (#1449) |
| DestinationRegion | 1 (#5) / Unknown Province Landing (#50) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2971 · 11 (#291) / Hyunmoon Temple - Door (#1451) / D2401 (#294) / Taoist Temple - Landing (#1477)

| 字段 | 值 |
|---|---|
| SourceRegion | 11 (#291) / Hyunmoon Temple - Door (#1451) |
| DestinationRegion | D2401 (#294) / Taoist Temple - Landing (#1477) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2972 · D2401 (#294) / Taoist Temple - Door (#1476) / 11 (#291) / Hyunmoon Temple - Landing (#1452)

| 字段 | 值 |
|---|---|
| SourceRegion | D2401 (#294) / Taoist Temple - Door (#1476) |
| DestinationRegion | 11 (#291) / Hyunmoon Temple - Landing (#1452) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2973 · D2401 (#294) / Hyunmoon Temple Lv 2 - Door (#1478) / D2402 (#295) / Hyunmoon Temple Lv 1 - Landing (#1482)

| 字段 | 值 |
|---|---|
| SourceRegion | D2401 (#294) / Hyunmoon Temple Lv 2 - Door (#1478) |
| DestinationRegion | D2402 (#295) / Hyunmoon Temple Lv 1 - Landing (#1482) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2974 · D2402 (#295) / Hyunmoon Temple Lv 1 - Door (#1481) / D2401 (#294) / Hyunmoon Temple Lv 2 - Landing (#1479)

| 字段 | 值 |
|---|---|
| SourceRegion | D2402 (#295) / Hyunmoon Temple Lv 1 - Door (#1481) |
| DestinationRegion | D2401 (#294) / Hyunmoon Temple Lv 2 - Landing (#1479) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2975 · D2402 (#295) / Hyunmoon Temple Lv 3 - Door (#1483) / D2403 (#296) / Hyunmoon Temple Lv 2 - Landing (#1487)

| 字段 | 值 |
|---|---|
| SourceRegion | D2402 (#295) / Hyunmoon Temple Lv 3 - Door (#1483) |
| DestinationRegion | D2403 (#296) / Hyunmoon Temple Lv 2 - Landing (#1487) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2976 · D2403 (#296) / Hyunmoon Temple Lv 2 - Door (#1486) / D2402 (#295) / Hyunmoon Temple Lv 3 - Landing (#1484)

| 字段 | 值 |
|---|---|
| SourceRegion | D2403 (#296) / Hyunmoon Temple Lv 2 - Door (#1486) |
| DestinationRegion | D2402 (#295) / Hyunmoon Temple Lv 3 - Landing (#1484) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2977 · 0 (#1) / Bichon Castle Entrance (#6) / 10 (#259) / Bichon Town - Landing (#1492)

| 字段 | 值 |
|---|---|
| SourceRegion | 0 (#1) / Bichon Castle Entrance (#6) |
| DestinationRegion | 10 (#259) / Bichon Town - Landing (#1492) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2978 · 10 (#259) / Bichon Town - Door (#1491) / 0 (#1) / Bichon Castle Landing (#7)

| 字段 | 值 |
|---|---|
| SourceRegion | 10 (#259) / Bichon Town - Door (#1491) |
| DestinationRegion | 0 (#1) / Bichon Castle Landing (#7) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2979 · 10 (#259) / Goru Cave - Door (#1493) / D2301 (#44) / Bichon Castle - Landing (#1514)

| 字段 | 值 |
|---|---|
| SourceRegion | 10 (#259) / Goru Cave - Door (#1493) |
| DestinationRegion | D2301 (#44) / Bichon Castle - Landing (#1514) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2980 · D2301 (#44) / Bichon Castle - Door (#1513) / 10 (#259) / Goru Cave - Landing (#1494)

| 字段 | 值 |
|---|---|
| SourceRegion | D2301 (#44) / Bichon Castle - Door (#1513) |
| DestinationRegion | 10 (#259) / Goru Cave - Landing (#1494) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2981 · D2301 (#44) / Goru Cave Lv 2 - Door (#1515) / D2302 (#260) / Goru Cave Lv 1 - Landing (#1519)

| 字段 | 值 |
|---|---|
| SourceRegion | D2301 (#44) / Goru Cave Lv 2 - Door (#1515) |
| DestinationRegion | D2302 (#260) / Goru Cave Lv 1 - Landing (#1519) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2982 · D2302 (#260) / Goru Cave Lv 1 - Door (#1518) / D2301 (#44) / Goru Cave Lv 2 - Landing (#1516)

| 字段 | 值 |
|---|---|
| SourceRegion | D2302 (#260) / Goru Cave Lv 1 - Door (#1518) |
| DestinationRegion | D2301 (#44) / Goru Cave Lv 2 - Landing (#1516) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2983 · D2302 (#260) / Goru Cave Lv 3 - Door (#1520) / D2303 (#261) / Goru Cave Lv 2 - Landing (#1524)

| 字段 | 值 |
|---|---|
| SourceRegion | D2302 (#260) / Goru Cave Lv 3 - Door (#1520) |
| DestinationRegion | D2303 (#261) / Goru Cave Lv 2 - Landing (#1524) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2984 · D2303 (#261) / Goru Cave Lv 2 - Door (#1523) / D2302 (#260) / Goru Cave Lv 3 - Landing (#1521)

| 字段 | 值 |
|---|---|
| SourceRegion | D2303 (#261) / Goru Cave Lv 2 - Door (#1523) |
| DestinationRegion | D2302 (#260) / Goru Cave Lv 3 - Landing (#1521) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2985 · D2303 (#261) / Goru Cave Lv 4 - Door (#1525) / D2304 (#262) / Goru Cave Lv 3 - Landing (#1531)

| 字段 | 值 |
|---|---|
| SourceRegion | D2303 (#261) / Goru Cave Lv 4 - Door (#1525) |
| DestinationRegion | D2304 (#262) / Goru Cave Lv 3 - Landing (#1531) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2986 · D2304 (#262) / Goru Cave Lv 3 - Door (#1530) / D2303 (#261) / Goru Cave Lv 4 - Landing (#1526)

| 字段 | 值 |
|---|---|
| SourceRegion | D2304 (#262) / Goru Cave Lv 3 - Door (#1530) |
| DestinationRegion | D2303 (#261) / Goru Cave Lv 4 - Landing (#1526) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2988 · 7 (#11) / Cave Door (#217) / D1802 (#121) / Infernal Island - Landing (#1538)

| 字段 | 值 |
|---|---|
| SourceRegion | 7 (#11) / Cave Door (#217) |
| DestinationRegion | D1802 (#121) / Infernal Island - Landing (#1538) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2989 · D1802 (#121) / Infernal Island - Entrance (#1537) / 7 (#11) / Cave Landing (#218)

| 字段 | 值 |
|---|---|
| SourceRegion | D1802 (#121) / Infernal Island - Entrance (#1537) |
| DestinationRegion | 7 (#11) / Cave Landing (#218) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2990 · 8 (#241) / Holy Palace Door (#835) / 8_002 (#280) / Forst Village Landing (#1542)

| 字段 | 值 |
|---|---|
| SourceRegion | 8 (#241) / Holy Palace Door (#835) |
| DestinationRegion | 8_002 (#280) / Forst Village Landing (#1542) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2991 · 8_002 (#280) / Frost Village Door (#1541) / 8 (#241) / Holy Palace Landing (#836)

| 字段 | 值 |
|---|---|
| SourceRegion | 8_002 (#280) / Frost Village Door (#1541) |
| DestinationRegion | 8 (#241) / Holy Palace Landing (#836) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2992 · 8_002 (#280) / Holy Palace Lv 1 - Door (#1543) / D2201 (#219) / Holy Palace Landing (#1546)

| 字段 | 值 |
|---|---|
| SourceRegion | 8_002 (#280) / Holy Palace Lv 1 - Door (#1543) |
| DestinationRegion | D2201 (#219) / Holy Palace Landing (#1546) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2993 · D2201 (#219) / Holy Palace Door (#1545) / 8_002 (#280) / Holy Palace Lv 1 - Landing (#1544)

| 字段 | 值 |
|---|---|
| SourceRegion | D2201 (#219) / Holy Palace Door (#1545) |
| DestinationRegion | 8_002 (#280) / Holy Palace Lv 1 - Landing (#1544) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2994 · D2201 (#219) / Holy Palace Lv 2 - Door (#1547) / D22021 (#273) / Holy Palace Lv 1 - Landing (#1556)

| 字段 | 值 |
|---|---|
| SourceRegion | D2201 (#219) / Holy Palace Lv 2 - Door (#1547) |
| DestinationRegion | D22021 (#273) / Holy Palace Lv 1 - Landing (#1556) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2995 · D22021 (#273) / Holy Palace Lv 1 - Door (#1555) / D2201 (#219) / Holy Palace Lv 2 - Landing (#1548)

| 字段 | 值 |
|---|---|
| SourceRegion | D22021 (#273) / Holy Palace Lv 1 - Door (#1555) |
| DestinationRegion | D2201 (#219) / Holy Palace Lv 2 - Landing (#1548) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2996 · D22021 (#273) / Holy Palace Lv 3 - Door (#1557) / D2204 (#277) / Holy Palace Lv 2 - Landing (#1560)

| 字段 | 值 |
|---|---|
| SourceRegion | D22021 (#273) / Holy Palace Lv 3 - Door (#1557) |
| DestinationRegion | D2204 (#277) / Holy Palace Lv 2 - Landing (#1560) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2997 · D2204 (#277) / Holy Palace Lv 2 - Door (#1559) / D22021 (#273) / Holy Palace Lv 3 - Landing (#1558)

| 字段 | 值 |
|---|---|
| SourceRegion | D2204 (#277) / Holy Palace Lv 2 - Door (#1559) |
| DestinationRegion | D22021 (#273) / Holy Palace Lv 3 - Landing (#1558) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2998 · D2204 (#277) / Holy Palace Lv 4 - Door (#1561) / D2205 (#278) / Holy Palace Lv 3 - Landing (#1563)

| 字段 | 值 |
|---|---|
| SourceRegion | D2204 (#277) / Holy Palace Lv 4 - Door (#1561) |
| DestinationRegion | D2205 (#278) / Holy Palace Lv 3 - Landing (#1563) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2999 · D006 (#332) / Lava Area Lv 2 - Door (#1571) / D007 (#333) / Lava Area Lv 1 - Landing (#1575)

| 字段 | 值 |
|---|---|
| SourceRegion | D006 (#332) / Lava Area Lv 2 - Door (#1571) |
| DestinationRegion | D007 (#333) / Lava Area Lv 1 - Landing (#1575) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3000 · D007 (#333) / Lava Area Lv 1 - Door (#1574) / D006 (#332) / Lava Area Lv 2 - Landing (#1572)

| 字段 | 值 |
|---|---|
| SourceRegion | D007 (#333) / Lava Area Lv 1 - Door (#1574) |
| DestinationRegion | D006 (#332) / Lava Area Lv 2 - Landing (#1572) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3001 · D007 (#333) / The Lair Entrance - Door (#1576) / D2900 (#334) / Lava Area Lv 2 - Landing (#1580)

| 字段 | 值 |
|---|---|
| SourceRegion | D007 (#333) / The Lair Entrance - Door (#1576) |
| DestinationRegion | D2900 (#334) / Lava Area Lv 2 - Landing (#1580) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3002 · D2900 (#334) / Lava Area Lv 2 - Door (#1579) / D007 (#333) / The Lair Entrance - Landing (#1577)

| 字段 | 值 |
|---|---|
| SourceRegion | D2900 (#334) / Lava Area Lv 2 - Door (#1579) |
| DestinationRegion | D007 (#333) / The Lair Entrance - Landing (#1577) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3003 · D2900 (#334) / The Lair Lv 1 - Door (#1581) / D2901 (#335) / The Lair Entrance - Landing (#1585)

| 字段 | 值 |
|---|---|
| SourceRegion | D2900 (#334) / The Lair Lv 1 - Door (#1581) |
| DestinationRegion | D2901 (#335) / The Lair Entrance - Landing (#1585) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3004 · D2901 (#335) / Tne Lair Entrance - Door (#1584) / D2900 (#334) / The Lair Lv 1 - Landing (#1582)

| 字段 | 值 |
|---|---|
| SourceRegion | D2901 (#335) / Tne Lair Entrance - Door (#1584) |
| DestinationRegion | D2900 (#334) / The Lair Lv 1 - Landing (#1582) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3005 · D2901 (#335) / The Lair Lv 2 West - Door (#1586) / D2902 (#336) / The Lair Lv 1 West - Landing (#1592)

| 字段 | 值 |
|---|---|
| SourceRegion | D2901 (#335) / The Lair Lv 2 West - Door (#1586) |
| DestinationRegion | D2902 (#336) / The Lair Lv 1 West - Landing (#1592) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3006 · D2901 (#335) / The Lair Lv 2 East - Door (#1588) / D2902 (#336) / The Lair Lv 1 East - Landing (#1594)

| 字段 | 值 |
|---|---|
| SourceRegion | D2901 (#335) / The Lair Lv 2 East - Door (#1588) |
| DestinationRegion | D2902 (#336) / The Lair Lv 1 East - Landing (#1594) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3007 · D2902 (#336) / The Lair Lv 1 West - Door (#1591) / D2901 (#335) / The Lair Lv 2 West - Landing (#1587)

| 字段 | 值 |
|---|---|
| SourceRegion | D2902 (#336) / The Lair Lv 1 West - Door (#1591) |
| DestinationRegion | D2901 (#335) / The Lair Lv 2 West - Landing (#1587) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3008 · D2902 (#336) / The Lair Lv 1 East - Door (#1593) / D2901 (#335) / The Lair Lv 2 East - Landing (#1589)

| 字段 | 值 |
|---|---|
| SourceRegion | D2902 (#336) / The Lair Lv 1 East - Door (#1593) |
| DestinationRegion | D2901 (#335) / The Lair Lv 2 East - Landing (#1589) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3009 · D2902 (#336) / The Lair Lv 3 West - Door (#1595) / D2904 (#339) / The Lair Lv 2 West - Landing (#1603)

| 字段 | 值 |
|---|---|
| SourceRegion | D2902 (#336) / The Lair Lv 3 West - Door (#1595) |
| DestinationRegion | D2904 (#339) / The Lair Lv 2 West - Landing (#1603) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3010 · D2902 (#336) / The Lair Lv 3 East - Door (#1597) / D2904 (#339) / The Lair Lv 2 East - Landing (#1605)

| 字段 | 值 |
|---|---|
| SourceRegion | D2902 (#336) / The Lair Lv 3 East - Door (#1597) |
| DestinationRegion | D2904 (#339) / The Lair Lv 2 East - Landing (#1605) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3011 · D2904 (#339) / The Lair Lv 2 West - Door (#1602) / D2902 (#336) / The Lair Lv 3 West - Landing (#1596)

| 字段 | 值 |
|---|---|
| SourceRegion | D2904 (#339) / The Lair Lv 2 West - Door (#1602) |
| DestinationRegion | D2902 (#336) / The Lair Lv 3 West - Landing (#1596) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3012 · D2904 (#339) / The Lair Lv 2 East - Door (#1604) / D2902 (#336) / The Lair Lv 3 East - Landing (#1598)

| 字段 | 值 |
|---|---|
| SourceRegion | D2904 (#339) / The Lair Lv 2 East - Door (#1604) |
| DestinationRegion | D2902 (#336) / The Lair Lv 3 East - Landing (#1598) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3013 · D2904 (#339) / The Lair Lv 4 West - Door (#1606) / D29051 (#340) / The Lair Lv 3 - Landing (#1613)

| 字段 | 值 |
|---|---|
| SourceRegion | D2904 (#339) / The Lair Lv 4 West - Door (#1606) |
| DestinationRegion | D29051 (#340) / The Lair Lv 3 - Landing (#1613) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3014 · D2904 (#339) / The Lair Lv 4 East - Door (#1608) / D29052 (#341) / The Lair Lv 3 - Landing (#1618)

| 字段 | 值 |
|---|---|
| SourceRegion | D2904 (#339) / The Lair Lv 4 East - Door (#1608) |
| DestinationRegion | D29052 (#341) / The Lair Lv 3 - Landing (#1618) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3015 · D29051 (#340) / The Lair Lv 5 - Door (#1614) / D2906 (#342) / The Lair Lv 4 West - Landing (#1623)

| 字段 | 值 |
|---|---|
| SourceRegion | D29051 (#340) / The Lair Lv 5 - Door (#1614) |
| DestinationRegion | D2906 (#342) / The Lair Lv 4 West - Landing (#1623) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3016 · D29052 (#341) / The Lair Lv 5 - Door (#1619) / D2906 (#342) / The Lair Lv 4 East - Landing (#1625)

| 字段 | 值 |
|---|---|
| SourceRegion | D29052 (#341) / The Lair Lv 5 - Door (#1619) |
| DestinationRegion | D2906 (#342) / The Lair Lv 4 East - Landing (#1625) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3017 · D2906 (#342) / The Lair Lv 6 - Door (#1626) / D2907 (#344) / Landing (#1629)

| 字段 | 值 |
|---|---|
| SourceRegion | D2906 (#342) / The Lair Lv 6 - Door (#1626) |
| DestinationRegion | D2907 (#344) / Landing (#1629) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3019 · D008 (#460) / Frost Village - Door (#1632) / 8 (#241) / Dragon Abyss Landing (#834)

| 字段 | 值 |
|---|---|
| SourceRegion | D008 (#460) / Frost Village - Door (#1632) |
| DestinationRegion | 8 (#241) / Dragon Abyss Landing (#834) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3020 · D008 (#460) / Dragon Abyss Lv 1 - Door (#1634) / D3001 (#461) / Dragon Abyss Ent - Landing (#1637)

| 字段 | 值 |
|---|---|
| SourceRegion | D008 (#460) / Dragon Abyss Lv 1 - Door (#1634) |
| DestinationRegion | D3001 (#461) / Dragon Abyss Ent - Landing (#1637) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3021 · D3001 (#461) / Dragon Abyss Ent - Door (#1636) / D008 (#460) / Dragon Abyss Lv 1 - Landing (#1635)

| 字段 | 值 |
|---|---|
| SourceRegion | D3001 (#461) / Dragon Abyss Ent - Door (#1636) |
| DestinationRegion | D008 (#460) / Dragon Abyss Lv 1 - Landing (#1635) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3022 · D3001 (#461) / Dragon Abyss Lv 2 - Door (#1640) / D3002 (#462) / Dragon Abyss Lv 1 - Landing (#1643)

| 字段 | 值 |
|---|---|
| SourceRegion | D3001 (#461) / Dragon Abyss Lv 2 - Door (#1640) |
| DestinationRegion | D3002 (#462) / Dragon Abyss Lv 1 - Landing (#1643) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3023 · D3002 (#462) / Dragon Abyss Lv 1 - Door (#1642) / D3001 (#461) / Dragon Abyss Lv 2 - Landing (#1641)

| 字段 | 值 |
|---|---|
| SourceRegion | D3002 (#462) / Dragon Abyss Lv 1 - Door (#1642) |
| DestinationRegion | D3001 (#461) / Dragon Abyss Lv 2 - Landing (#1641) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3024 · D3002 (#462) / Dragon Abyss Lv 3- Door (#1644) / D3004 (#466) / Dragon Abyss Lv 2 - Landing (#1649)

| 字段 | 值 |
|---|---|
| SourceRegion | D3002 (#462) / Dragon Abyss Lv 3- Door (#1644) |
| DestinationRegion | D3004 (#466) / Dragon Abyss Lv 2 - Landing (#1649) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3025 · D3004 (#466) / Dragon Abyss Lv 2 - Door (#1648) / D3002 (#462) / Dragon Abyss Lv 3 - Landing (#1645)

| 字段 | 值 |
|---|---|
| SourceRegion | D3004 (#466) / Dragon Abyss Lv 2 - Door (#1648) |
| DestinationRegion | D3002 (#462) / Dragon Abyss Lv 3 - Landing (#1645) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3026 · D3004 (#466) / Dragon Abyss Lv 4 - Door (#1650) / D3005 (#470) / Dragon Abyss Lv 3 - Landing (#1654)

| 字段 | 值 |
|---|---|
| SourceRegion | D3004 (#466) / Dragon Abyss Lv 4 - Door (#1650) |
| DestinationRegion | D3005 (#470) / Dragon Abyss Lv 3 - Landing (#1654) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3027 · D3005 (#470) / Dragon Abyss Lv 3 - Door (#1653) / D3004 (#466) / Dragon Abyss Lv 4 - Landing (#1651)

| 字段 | 值 |
|---|---|
| SourceRegion | D3005 (#470) / Dragon Abyss Lv 3 - Door (#1653) |
| DestinationRegion | D3004 (#466) / Dragon Abyss Lv 4 - Landing (#1651) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3028 · D3005 (#470) / Dragon Abyss Lv 5 NW - Door (#1655) / D3005_BH (#601) / Dragon Abyss 4th - Landing (#1675)

| 字段 | 值 |
|---|---|
| SourceRegion | D3005 (#470) / Dragon Abyss Lv 5 NW - Door (#1655) |
| DestinationRegion | D3005_BH (#601) / Dragon Abyss 4th - Landing (#1675) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3029 · D3005 (#470) / Dragon Abyss Lv 5 NE - Door (#1657) / D3005_CR (#602) / Dragon Abyss 4th - Landing (#1678)

| 字段 | 值 |
|---|---|
| SourceRegion | D3005 (#470) / Dragon Abyss Lv 5 NE - Door (#1657) |
| DestinationRegion | D3005_CR (#602) / Dragon Abyss 4th - Landing (#1678) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3030 · D3005 (#470) / Dragon Abyss Lv 5 SW - Door (#1659) / D3005_HM (#603) / Dragon Abyss 4th - Landing (#1681)

| 字段 | 值 |
|---|---|
| SourceRegion | D3005 (#470) / Dragon Abyss Lv 5 SW - Door (#1659) |
| DestinationRegion | D3005_HM (#603) / Dragon Abyss 4th - Landing (#1681) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3031 · D3005 (#470) / Dragon Abyss Lv 5 SE - Door (#1661) / D3005_JJ (#604) / Dragon Abyss 4th - Landing (#1684)

| 字段 | 值 |
|---|---|
| SourceRegion | D3005 (#470) / Dragon Abyss Lv 5 SE - Door (#1661) |
| DestinationRegion | D3005_JJ (#604) / Dragon Abyss 4th - Landing (#1684) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3032 · D3005 (#470) / Dragon Abyss Lv 6 - Door (#1663) / D3006 (#480) / Dragon Abyss 4th - Landing (#1668)

| 字段 | 值 |
|---|---|
| SourceRegion | D3005 (#470) / Dragon Abyss Lv 6 - Door (#1663) |
| DestinationRegion | D3006 (#480) / Dragon Abyss 4th - Landing (#1668) |
| Icon | Down |
| NeedItem | Ancestral Tablet Of Sama Mage (#954) |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3034 · 8 (#241) / Dragon Abyss Door (#833) / D008 (#460) / Frost Village  - Landing (#1633)

| 字段 | 值 |
|---|---|
| SourceRegion | 8 (#241) / Dragon Abyss Door (#833) |
| DestinationRegion | D008 (#460) / Frost Village  - Landing (#1633) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3035 · D3400 (#605) / Lost Land 2 - Left Door (#1695) / D3400_1 (#606) / Lost Land - Left Landing (#1700)

| 字段 | 值 |
|---|---|
| SourceRegion | D3400 (#605) / Lost Land 2 - Left Door (#1695) |
| DestinationRegion | D3400_1 (#606) / Lost Land - Left Landing (#1700) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3036 · D3400 (#605) / Lost Land 2 - Right Door (#1697) / D3400_1 (#606) / Lost Land - Right Landing (#1702)

| 字段 | 值 |
|---|---|
| SourceRegion | D3400 (#605) / Lost Land 2 - Right Door (#1697) |
| DestinationRegion | D3400_1 (#606) / Lost Land - Right Landing (#1702) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3037 · D3400_1 (#606) / Lost Land - Left Door (#1699) / D3400 (#605) / Lost Land 2 - Left Landing (#1696)

| 字段 | 值 |
|---|---|
| SourceRegion | D3400_1 (#606) / Lost Land - Left Door (#1699) |
| DestinationRegion | D3400 (#605) / Lost Land 2 - Left Landing (#1696) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3038 · D3400_1 (#606) / Lost Land - Right Door (#1701) / D3400 (#605) / Lost Land 2 - Right Landing (#1698)

| 字段 | 值 |
|---|---|
| SourceRegion | D3400_1 (#606) / Lost Land - Right Door (#1701) |
| DestinationRegion | D3400 (#605) / Lost Land 2 - Right Landing (#1698) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3039 · D3400_1 (#606) / Lost Land 3 - Door (#1715) / ER51_Ice (#607) / Lost Land 2 - Landing (#1714)

| 字段 | 值 |
|---|---|
| SourceRegion | D3400_1 (#606) / Lost Land 3 - Door (#1715) |
| DestinationRegion | ER51_Ice (#607) / Lost Land 2 - Landing (#1714) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3040 · ER51_Ice (#607) / Lost Land 2 - Door (#1713) / D3400_1 (#606) / Lost Land 3 - Landing (#1716)

| 字段 | 值 |
|---|---|
| SourceRegion | ER51_Ice (#607) / Lost Land 2 - Door (#1713) |
| DestinationRegion | D3400_1 (#606) / Lost Land 3 - Landing (#1716) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3041 · D4003 (#590) / The Wall 1 - Door (#1719) / ID3_014 (#608) / Southern Check Point - Landing (#1722)

| 字段 | 值 |
|---|---|
| SourceRegion | D4003 (#590) / The Wall 1 - Door (#1719) |
| DestinationRegion | ID3_014 (#608) / Southern Check Point - Landing (#1722) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3042 · ID3_014 (#608) / Southern Check Point - Door (#1721) / D4003 (#590) / The Wall 1 - Landing (#1720)

| 字段 | 值 |
|---|---|
| SourceRegion | ID3_014 (#608) / Southern Check Point - Door (#1721) |
| DestinationRegion | D4003 (#590) / The Wall 1 - Landing (#1720) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3043 · ID3_014 (#608) / The Wall 2 - Door (#1723) / ID3_024 (#609) / The Wall - Landing (#1726)

| 字段 | 值 |
|---|---|
| SourceRegion | ID3_014 (#608) / The Wall 2 - Door (#1723) |
| DestinationRegion | ID3_024 (#609) / The Wall - Landing (#1726) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3044 · ID3_024 (#609) / The Wall - Door (#1725) / ID3_014 (#608) / The Wall 2 - Landing (#1724)

| 字段 | 值 |
|---|---|
| SourceRegion | ID3_024 (#609) / The Wall - Door (#1725) |
| DestinationRegion | ID3_014 (#608) / The Wall 2 - Landing (#1724) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3045 · D3005_BH (#601) / Dragon Abyss 4th -  Door (#1674) / D3005 (#470) / Dragon Abyss Lv 5 NW - Landing (#1656)

| 字段 | 值 |
|---|---|
| SourceRegion | D3005_BH (#601) / Dragon Abyss 4th -  Door (#1674) |
| DestinationRegion | D3005 (#470) / Dragon Abyss Lv 5 NW - Landing (#1656) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3046 · D3005_CR (#602) / Dragon Abyss 4th -  Door (#1677) / D3005 (#470) / Dragon Abyss Lv 5 NE - Landing (#1658)

| 字段 | 值 |
|---|---|
| SourceRegion | D3005_CR (#602) / Dragon Abyss 4th -  Door (#1677) |
| DestinationRegion | D3005 (#470) / Dragon Abyss Lv 5 NE - Landing (#1658) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3047 · D3005_HM (#603) / Dragon Abyss 4th -  Door (#1680) / D3005 (#470) / Dragon Abyss Lv 5 SW - Landing (#1660)

| 字段 | 值 |
|---|---|
| SourceRegion | D3005_HM (#603) / Dragon Abyss 4th -  Door (#1680) |
| DestinationRegion | D3005 (#470) / Dragon Abyss Lv 5 SW - Landing (#1660) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #3048 · D3005_JJ (#604) / Dragon Abyss 4th -  Door (#1683) / D3005 (#470) / Dragon Abyss Lv 5 SE - Landing (#1662)

| 字段 | 值 |
|---|---|
| SourceRegion | D3005_JJ (#604) / Dragon Abyss 4th -  Door (#1683) |
| DestinationRegion | D3005 (#470) / Dragon Abyss Lv 5 SE - Landing (#1662) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

