<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改。重新生成: dotnet run --project Tools/SystemDbProbe -- --dump docs/database -->

# 刷新点（RespawnInfo）

> 记录 #5108 – #5417，共 1471 条（第 4/5 部分）。

[README](../README.md) · [← 上一部分](RespawnInfo.3.md) · [下一部分 →](RespawnInfo.5.md)

## 快速浏览

| # | Monster | Region | Delay | Count | DropSet | EventSpawn |
|---|---|---|---|---|---|---|
| 5108 | Corpse Devourer (#44) | 4 (#8) / Spawn Ring 2 (#928) | 1 | 300 | 0 | false |
| 5109 | Visceral Worm (#45) | 4 (#8) / Spawn Ring 2 (#928) | 1 | 300 | 0 | false |
| 5110 | Beetle (#43) | 4 (#8) / Spawn Ring 2 (#928) | 1 | 300 | 0 | false |
| 5111 | Spiked Beetle (#88) | 4 (#8) / Spawn Ring 2 (#928) | 30 | 2 | 0 | false |
| 5112 | Numa Cavalry (#161) | D1501 (#74) / Whole Map (#929) | 1 | 60 | 0 | false |
| 5113 | Numa High Mage (#162) | D1501 (#74) / Whole Map (#929) | 1 | 60 | 0 | false |
| 5114 | Numa Stone Thrower (#163) | D1501 (#74) / Whole Map (#929) | 1 | 30 | 0 | false |
| 5116 | Numa Royal Guard (#164) | D1501 (#74) / Whole Map (#929) | 1 | 80 | 0 | false |
| 5117 | Numa Armored Soldier (#165) | D1501 (#74) / Whole Map (#929) | 1 | 80 | 0 | false |
| 5118 | Numa Cavalry (#161) | D1502 (#75) / Whole Map (#940) | 1 | 70 | 0 | false |
| 5119 | Numa High Mage (#162) | D1502 (#75) / Whole Map (#940) | 1 | 70 | 0 | false |
| 5120 | Numa Stone Thrower (#163) | D1502 (#75) / Whole Map (#940) | 1 | 40 | 0 | false |
| 5121 | Numa Royal Guard (#164) | D1502 (#75) / Whole Map (#940) | 1 | 120 | 0 | false |
| 5122 | Numa Armored Soldier (#165) | D1502 (#75) / Whole Map (#940) | 1 | 120 | 0 | false |
| 5123 | Numa Cavalry (#161) | D1502 (#75) / Respawn Area (#951) | 15 | 20 | 0 | false |
| 5124 | Numa High Mage (#162) | D1502 (#75) / Respawn Area (#951) | 15 | 20 | 0 | false |
| 5125 | Numa Stone Thrower (#163) | D1502 (#75) / Respawn Area (#951) | 15 | 10 | 0 | false |
| 5126 | Numa Royal Guard (#164) | D1502 (#75) / Respawn Area (#951) | 15 | 30 | 0 | false |
| 5127 | Numa Armored Soldier (#165) | D1502 (#75) / Respawn Area (#951) | 15 | 30 | 0 | false |
| 5128 | Numa Cavalry (#161) | D15031 (#76) / Whole Map (#952) | 1 | 70 | 0 | false |
| 5129 | Numa High Mage (#162) | D15031 (#76) / Whole Map (#952) | 1 | 70 | 0 | false |
| 5130 | Numa Stone Thrower (#163) | D15031 (#76) / Whole Map (#952) | 1 | 40 | 0 | false |
| 5131 | Numa Royal Guard (#164) | D15031 (#76) / Whole Map (#952) | 1 | 200 | 0 | false |
| 5132 | Numa Armored Soldier (#165) | D15031 (#76) / Whole Map (#952) | 1 | 200 | 0 | false |
| 5133 | Numa Cavalry (#161) | D15032 (#77) / Whole Map (#957) | 1 | 70 | 0 | false |
| 5134 | Numa High Mage (#162) | D15032 (#77) / Whole Map (#957) | 1 | 70 | 0 | false |
| 5135 | Numa Stone Thrower (#163) | D15032 (#77) / Whole Map (#957) | 1 | 40 | 0 | false |
| 5136 | Numa Royal Guard (#164) | D15032 (#77) / Whole Map (#957) | 1 | 200 | 0 | false |
| 5137 | Numa Armored Soldier (#165) | D15032 (#77) / Whole Map (#957) | 1 | 200 | 0 | false |
| 5139 | Numa Cavalry (#161) | D15033 (#78) / Whole Map (#962) | 1 | 70 | 0 | false |
| 5140 | Numa High Mage (#162) | D15033 (#78) / Whole Map (#962) | 1 | 70 | 0 | false |
| 5141 | Numa Stone Thrower (#163) | D15033 (#78) / Whole Map (#962) | 1 | 40 | 0 | false |
| 5142 | Numa Royal Guard (#164) | D15033 (#78) / Whole Map (#962) | 1 | 200 | 0 | false |
| 5143 | Numa Armored Soldier (#165) | D15033 (#78) / Whole Map (#962) | 1 | 200 | 0 | false |
| 5144 | Numa Cavalry (#161) | D15034 (#79) / Whole Map (#967) | 1 | 70 | 0 | false |
| 5145 | Numa High Mage (#162) | D15034 (#79) / Whole Map (#967) | 1 | 70 | 0 | false |
| 5146 | Numa Stone Thrower (#163) | D15034 (#79) / Whole Map (#967) | 1 | 40 | 0 | false |
| 5147 | Numa Royal Guard (#164) | D15034 (#79) / Whole Map (#967) | 1 | 200 | 0 | false |
| 5148 | Numa Armored Soldier (#165) | D15034 (#79) / Whole Map (#967) | 1 | 200 | 0 | false |
| 5149 | Numa Cavalry (#161) | D1504 (#80) / Top Area (#972) | 15 | 35 | 0 | false |
| 5150 | Numa High Mage (#162) | D1504 (#80) / Top Area (#972) | 15 | 35 | 0 | false |
| 5151 | Numa Stone Thrower (#163) | D1504 (#80) / Top Area (#972) | 15 | 20 | 0 | false |
| 5152 | Numa Royal Guard (#164) | D1504 (#80) / Top Area (#972) | 15 | 100 | 0 | false |
| 5153 | Numa Armored Soldier (#165) | D1504 (#80) / Top Area (#972) | 15 | 100 | 0 | false |
| 5155 | Numa Cavalry (#161) | D1504 (#80) / Left Area (#973) | 30 | 20 | 0 | false |
| 5156 | Numa High Mage (#162) | D1504 (#80) / Left Area (#973) | 30 | 20 | 0 | false |
| 5157 | Numa Stone Thrower (#163) | D1504 (#80) / Left Area (#973) | 30 | 15 | 0 | false |
| 5158 | Numa Royal Guard (#164) | D1504 (#80) / Left Area (#973) | 30 | 70 | 0 | false |
| 5159 | Numa Armored Soldier (#165) | D1504 (#80) / Left Area (#973) | 30 | 70 | 0 | false |
| 5161 | Numa Cavalry (#161) | D1504 (#80) / Right Area (#974) | 30 | 20 | 0 | false |
| 5162 | Numa High Mage (#162) | D1504 (#80) / Right Area (#974) | 30 | 20 | 0 | false |
| 5163 | Numa Stone Thrower (#163) | D1504 (#80) / Right Area (#974) | 30 | 15 | 0 | false |
| 5164 | Numa Royal Guard (#164) | D1504 (#80) / Right Area (#974) | 30 | 70 | 0 | false |
| 5165 | Numa Armored Soldier (#165) | D1504 (#80) / Right Area (#974) | 30 | 70 | 0 | false |
| 5166 | Numa Cavalry (#161) | D1505 (#81) / Row 1 Area (#993) | 15 | 10 | 0 | false |
| 5167 | Numa High Mage (#162) | D1505 (#81) / Row 1 Area (#993) | 15 | 10 | 0 | false |
| 5168 | Numa Stone Thrower (#163) | D1505 (#81) / Row 1 Area (#993) | 15 | 5 | 0 | false |
| 5169 | Numa Royal Guard (#164) | D1505 (#81) / Row 1 Area (#993) | 15 | 40 | 0 | false |
| 5170 | Numa Armored Soldier (#165) | D1505 (#81) / Row 1 Area (#993) | 15 | 40 | 0 | false |
| 5171 | Numa Cavalry (#161) | D1505 (#81) / Row 2 Area (#994) | 15 | 15 | 0 | false |
| 5172 | Numa High Mage (#162) | D1505 (#81) / Row 2 Area (#994) | 15 | 15 | 0 | false |
| 5173 | Numa Stone Thrower (#163) | D1505 (#81) / Row 2 Area (#994) | 15 | 10 | 0 | false |
| 5174 | Numa Royal Guard (#164) | D1505 (#81) / Row 2 Area (#994) | 15 | 50 | 0 | false |
| 5175 | Numa Armored Soldier (#165) | D1505 (#81) / Row 2 Area (#994) | 15 | 50 | 0 | false |
| 5176 | Numa Cavalry (#161) | D1505 (#81) / Row 3 Area (#995) | 15 | 20 | 0 | false |
| 5177 | Numa High Mage (#162) | D1505 (#81) / Row 3 Area (#995) | 15 | 20 | 0 | false |
| 5178 | Numa Stone Thrower (#163) | D1505 (#81) / Row 3 Area (#995) | 15 | 15 | 0 | false |
| 5179 | Numa Royal Guard (#164) | D1505 (#81) / Row 3 Area (#995) | 15 | 60 | 0 | false |
| 5180 | Numa Armored Soldier (#165) | D1505 (#81) / Row 3 Area (#995) | 15 | 60 | 0 | false |
| 5181 | Numa Cavalry (#161) | D1505 (#81) / Row 4 Area (#996) | 15 | 25 | 0 | false |
| 5182 | Numa High Mage (#162) | D1505 (#81) / Row 4 Area (#996) | 15 | 25 | 0 | false |
| 5183 | Numa Stone Thrower (#163) | D1505 (#81) / Row 4 Area (#996) | 15 | 20 | 0 | false |
| 5184 | Numa Royal Guard (#164) | D1505 (#81) / Row 4 Area (#996) | 15 | 70 | 0 | false |
| 5185 | Numa Armored Soldier (#165) | D1505 (#81) / Row 4 Area (#996) | 15 | 70 | 0 | false |
| 5186 | Numa Armored Soldier (#165) | D1505 (#81) / Bottom Left Area (#1007) | 15 | 50 | 0 | false |
| 5187 | Numa Armored Soldier (#165) | D1505 (#81) / Bottom Right Area (#1008) | 15 | 50 | 0 | false |
| 5188 | Numa Royal Guard (#164) | D1505 (#81) / Bottom Left Area (#1007) | 15 | 50 | 0 | false |
| 5189 | Numa Royal Guard (#164) | D1505 (#81) / Bottom Right Area (#1008) | 15 | 50 | 0 | false |
| 5190 | Numa Cavalry (#161) | D1505 (#81) / Bottom Left Area (#1007) | 15 | 50 | 0 | false |
| 5191 | Numa Cavalry (#161) | D1505 (#81) / Bottom Right Area (#1008) | 15 | 50 | 0 | false |
| 5192 | Numa Stone Thrower (#163) | D1505 (#81) / Bottom Left Area (#1007) | 15 | 50 | 0 | false |
| 5193 | Numa Stone Thrower (#163) | D1505 (#81) / Bottom Right Area (#1008) | 15 | 50 | 0 | false |
| 5194 | Numa High Mage (#162) | D1505 (#81) / Bottom Left Area (#1007) | 15 | 50 | 0 | false |
| 5195 | Numa High Mage (#162) | D1505 (#81) / Bottom Right Area (#1008) | 15 | 50 | 0 | false |
| 5196 | Numa Assault Captain (#166) | D1505 (#81) / Bottom Left Area (#1007) | 30 | 1 | 0 | false |
| 5197 | Numa Assault Captain (#166) | D1505 (#81) / Bottom Right Area (#1008) | 30 | 1 | 0 | false |
| 5198 | Decaying Ghoul (#58) | 12 (#292) / Spawn Area (#1125) | 1 | 450 | 0 | false |
| 5199 | Rotting Ghoul (#57) | 12 (#292) / Spawn Area (#1125) | 1 | 450 | 0 | false |
| 5200 | Blood Thristy Zombie (#60) | 12 (#292) / Spawn Area (#1125) | 60 | 4 | 0 | false |
| 5201 | Bloody Armed Beetle (#179) | D2501 (#298) / Whole Map (#1128) | 1 | 75 | 0 | false |
| 5202 | Earwig King (#181) | D2501 (#298) / Whole Map (#1128) | 1 | 50 | 0 | false |
| 5203 | Bloody Armed Beetle (#179) | D2502 (#299) / Whole Map (#1133) | 1 | 110 | 0 | false |
| 5204 | Earwig King (#181) | D2502 (#299) / Whole Map (#1133) | 1 | 80 | 0 | false |
| 5205 | Bloody Armed Beetle (#179) | D2503 (#300) / Whole Map (#1138) | 1 | 140 | 0 | false |
| 5206 | Earwig King (#181) | D2503 (#300) / Whole Map (#1138) | 1 | 110 | 0 | false |
| 5207 | Enraged Lord Ji'Nae (#184) | D2503 (#300) / Whole Map (#1138) | 150 | 1 | 0 | false |
| 5208 | Bloody Armed Beetle (#179) | D2503 (#300) / Whole Map (#1138) | 15 | 20 | 0 | false |
| 5209 | Earwig King (#181) | D2503 (#300) / Whole Map (#1138) | 15 | 20 | 0 | false |
| 5210 | Banyo Soldier (#185) | 13 (#293) / Whole Map (#1144) | 1 | 125 | 0 | false |
| 5211 | Banyo Warrior (#186) | 13 (#293) / Whole Map (#1144) | 1 | 48 | 0 | false |
| 5214 | Banyo Soldier (#185) | D2601 (#301) / Spawn Area (#1149) | 1 | 555 | 1 | false |
| 5215 | Banyo Warrior (#186) | D2601 (#301) / Spawn Area (#1149) | 1 | 333 | 1 | false |
| 5216 | Banyo Captain (#187) | D2601 (#301) / Spawn Area (#1149) | 1 | 600 | 1 | false |
| 5217 | Banyo Lord Guzak (#188) | D2601 (#301) / Spawn Area (#1149) | 210 | 1 | 0 | false |
| 5218 | Pink Goddess Of Black Palace (#130) | D1301 (#62) / Whole Map (#1159) | 1 | 80 | 0 | false |
| 5219 | Green Goddess Of Black Palace (#131) | D1301 (#62) / Whole Map (#1159) | 1 | 80 | 0 | false |
| 5220 | Stone Griffin (#133) | D1301 (#62) / Whole Map (#1159) | 1 | 30 | 1 | false |
| 5221 | Flame Griffin (#134) | D1301 (#62) / Whole Map (#1159) | 1 | 50 | 1 | false |
| 5222 | Pink Goddess Of Black Palace (#130) | D13021 (#63) / Whole Map (#1168) | 1 | 120 | 0 | false |
| 5223 | Green Goddess Of Black Palace (#131) | D13021 (#63) / Whole Map (#1168) | 1 | 120 | 0 | false |
| 5224 | Stone Griffin (#133) | D13021 (#63) / Whole Map (#1168) | 1 | 50 | 1 | false |
| 5225 | Flame Griffin (#134) | D13021 (#63) / Whole Map (#1168) | 1 | 60 | 1 | false |
| 5226 | Pink Goddess Of Black Palace (#130) | D13022 (#64) / Whole Map (#1173) | 1 | 120 | 0 | false |
| 5227 | Green Goddess Of Black Palace (#131) | D13022 (#64) / Whole Map (#1173) | 1 | 120 | 0 | false |
| 5228 | Stone Griffin (#133) | D13022 (#64) / Whole Map (#1173) | 1 | 50 | 1 | false |
| 5229 | Flame Griffin (#134) | D13022 (#64) / Whole Map (#1173) | 1 | 60 | 1 | false |
| 5230 | Pink Goddess Of Black Palace (#130) | D1303 (#65) / Whole Map (#1178) | 1 | 150 | 0 | false |
| 5231 | Green Goddess Of Black Palace (#131) | D1303 (#65) / Whole Map (#1178) | 1 | 150 | 0 | false |
| 5232 | Stone Griffin (#133) | D1303 (#65) / Whole Map (#1178) | 1 | 50 | 1 | false |
| 5233 | Flame Griffin (#134) | D1303 (#65) / Whole Map (#1178) | 1 | 80 | 1 | false |
| 5234 | Mutant Captain (#132) | D1303 (#65) / Whole Map (#1178) | 1 | 150 | 0 | false |
| 5235 | Pink Goddess Of Black Palace (#130) | D1304 (#66) / Whole Map (#1185) | 1 | 150 | 0 | false |
| 5236 | Green Goddess Of Black Palace (#131) | D1304 (#66) / Whole Map (#1185) | 1 | 150 | 0 | false |
| 5237 | Stone Griffin (#133) | D1304 (#66) / Whole Map (#1185) | 1 | 50 | 1 | false |
| 5238 | Flame Griffin (#134) | D1304 (#66) / Whole Map (#1185) | 1 | 80 | 1 | false |
| 5239 | Mutant Captain (#132) | D1304 (#66) / Whole Map (#1185) | 1 | 150 | 0 | false |
| 5240 | Black Palace Warlord (#135) | D1304 (#66) / Whole Map (#1185) | 30 | 2 | 0 | false |
| 5241 | Pink Goddess Of Underground (#136) | D1200 (#42) / Whole Map (#1194) | 1 | 120 | 0 | false |
| 5242 | Green Goddess Of Underground (#138) | D1200 (#42) / Whole Map (#1194) | 1 | 120 | 0 | false |
| 5243 | Stone Griffin (#133) | D1200 (#42) / Whole Map (#1194) | 1 | 60 | 2 | false |
| 5244 | Flame Griffin (#134) | D1200 (#42) / Whole Map (#1194) | 1 | 60 | 2 | false |
| 5245 | Pink Goddess Of Underground (#136) | D12011 (#43) / Whole Map (#1207) | 1 | 100 | 0 | false |
| 5246 | Green Goddess Of Underground (#138) | D12011 (#43) / Whole Map (#1207) | 1 | 100 | 0 | false |
| 5247 | Stone Griffin (#133) | D12011 (#43) / Whole Map (#1207) | 1 | 40 | 2 | false |
| 5248 | Flame Griffin (#134) | D12011 (#43) / Whole Map (#1207) | 1 | 40 | 2 | false |
| 5249 | Pink Goddess Of Underground (#136) | D12012 (#45) / Whole Map (#1212) | 1 | 200 | 0 | false |
| 5250 | Green Goddess Of Underground (#138) | D12012 (#45) / Whole Map (#1212) | 1 | 200 | 0 | false |
| 5251 | Stone Griffin (#133) | D12012 (#45) / Whole Map (#1212) | 1 | 80 | 2 | false |
| 5252 | Flame Griffin (#134) | D12012 (#45) / Whole Map (#1212) | 1 | 80 | 2 | false |
| 5254 | Pink Goddess Of Underground (#136) | D12013 (#46) / Whole Map (#1215) | 1 | 120 | 0 | false |
| 5255 | Green Goddess Of Underground (#138) | D12013 (#46) / Whole Map (#1215) | 1 | 120 | 0 | false |
| 5256 | Stone Griffin (#133) | D12013 (#46) / Whole Map (#1215) | 1 | 50 | 2 | false |
| 5257 | Flame Griffin (#134) | D12013 (#46) / Whole Map (#1215) | 1 | 50 | 2 | false |
| 5258 | Pink Goddess Of Underground (#136) | D12014 (#47) / Whole Map (#1220) | 1 | 150 | 0 | false |
| 5259 | Green Goddess Of Underground (#138) | D12014 (#47) / Whole Map (#1220) | 1 | 150 | 0 | false |
| 5260 | Stone Griffin (#133) | D12014 (#47) / Whole Map (#1220) | 1 | 50 | 2 | false |
| 5261 | Flame Griffin (#134) | D12014 (#47) / Whole Map (#1220) | 1 | 50 | 2 | false |
| 5262 | Pink Goddess Of Underground (#136) | D12021 (#48) / Whole Map (#1225) | 1 | 130 | 0 | false |
| 5263 | Green Goddess Of Underground (#138) | D12021 (#48) / Whole Map (#1225) | 1 | 130 | 0 | false |
| 5264 | Stone Griffin (#133) | D12021 (#48) / Whole Map (#1225) | 1 | 60 | 2 | false |
| 5265 | Flame Griffin (#134) | D12021 (#48) / Whole Map (#1225) | 1 | 60 | 2 | false |
| 5266 | Vicious Mutant Captain (#137) | D12021 (#48) / Whole Map (#1225) | 1 | 80 | 0 | false |
| 5267 | Pink Goddess Of Underground (#136) | D12022 (#49) / Whole Map (#1230) | 1 | 130 | 0 | false |
| 5268 | Green Goddess Of Underground (#138) | D12022 (#49) / Whole Map (#1230) | 1 | 130 | 0 | false |
| 5269 | Stone Griffin (#133) | D12022 (#49) / Whole Map (#1230) | 1 | 60 | 2 | false |
| 5270 | Flame Griffin (#134) | D12022 (#49) / Whole Map (#1230) | 1 | 60 | 2 | false |
| 5271 | Vicious Mutant Captain (#137) | D12022 (#49) / Whole Map (#1230) | 1 | 80 | 0 | false |
| 5272 | Pink Goddess Of Underground (#136) | D12023 (#50) / Whole Map (#1235) | 1 | 160 | 0 | false |
| 5273 | Green Goddess Of Underground (#138) | D12023 (#50) / Whole Map (#1235) | 1 | 160 | 0 | false |
| 5274 | Stone Griffin (#133) | D12023 (#50) / Whole Map (#1235) | 1 | 70 | 2 | false |
| 5275 | Flame Griffin (#134) | D12023 (#50) / Whole Map (#1235) | 1 | 70 | 2 | false |
| 5276 | Vicious Mutant Captain (#137) | D12023 (#50) / Whole Map (#1235) | 1 | 100 | 0 | false |
| 5277 | Pink Goddess Of Underground (#136) | D12024 (#51) / Whole Map (#1240) | 1 | 130 | 0 | false |
| 5278 | Green Goddess Of Underground (#138) | D12024 (#51) / Whole Map (#1240) | 1 | 130 | 0 | false |
| 5279 | Stone Griffin (#133) | D12024 (#51) / Whole Map (#1240) | 1 | 60 | 2 | false |
| 5280 | Flame Griffin (#134) | D12024 (#51) / Whole Map (#1240) | 1 | 60 | 2 | false |
| 5281 | Vicious Mutant Captain (#137) | D12024 (#51) / Whole Map (#1240) | 1 | 80 | 0 | false |
| 5282 | Pink Goddess Of Underground (#136) | D12033 (#54) / Whole Map (#1245) | 1 | 180 | 0 | false |
| 5283 | Green Goddess Of Underground (#138) | D12033 (#54) / Whole Map (#1245) | 1 | 180 | 0 | false |
| 5284 | Stone Griffin (#133) | D12033 (#54) / Whole Map (#1245) | 1 | 60 | 2 | false |
| 5285 | Flame Griffin (#134) | D12033 (#54) / Whole Map (#1245) | 1 | 60 | 2 | false |
| 5286 | Vicious Mutant Captain (#137) | D12033 (#54) / Whole Map (#1245) | 1 | 120 | 0 | false |
| 5287 | Pink Goddess Of Underground (#136) | D12031 (#52) / Whole Map (#1248) | 1 | 130 | 0 | false |
| 5288 | Green Goddess Of Underground (#138) | D12031 (#52) / Whole Map (#1248) | 1 | 130 | 0 | false |
| 5289 | Stone Griffin (#133) | D12031 (#52) / Whole Map (#1248) | 1 | 60 | 2 | false |
| 5290 | Flame Griffin (#134) | D12031 (#52) / Whole Map (#1248) | 1 | 60 | 2 | false |
| 5291 | Vicious Mutant Captain (#137) | D12031 (#52) / Whole Map (#1248) | 1 | 80 | 0 | false |
| 5292 | Pink Goddess Of Underground (#136) | D12032 (#53) / Whole Map (#1253) | 1 | 160 | 0 | false |
| 5293 | Green Goddess Of Underground (#138) | D12032 (#53) / Whole Map (#1253) | 1 | 160 | 0 | false |
| 5294 | Stone Griffin (#133) | D12032 (#53) / Whole Map (#1253) | 1 | 80 | 2 | false |
| 5295 | Flame Griffin (#134) | D12032 (#53) / Whole Map (#1253) | 1 | 80 | 2 | false |
| 5296 | Vicious Mutant Captain (#137) | D12032 (#53) / Whole Map (#1253) | 1 | 120 | 0 | false |
| 5297 | Pink Goddess Of Underground (#136) | D12041 (#55) / Whole Map (#1260) | 1 | 200 | 0 | false |
| 5298 | Green Goddess Of Underground (#138) | D12041 (#55) / Whole Map (#1260) | 1 | 200 | 0 | false |
| 5299 | Stone Griffin (#133) | D12041 (#55) / Whole Map (#1260) | 1 | 80 | 2 | false |
| 5300 | Flame Griffin (#134) | D12041 (#55) / Whole Map (#1260) | 1 | 80 | 2 | false |
| 5301 | Vicious Mutant Captain (#137) | D12041 (#55) / Whole Map (#1260) | 1 | 150 | 0 | false |
| 5302 | Pink Goddess Of Underground (#136) | D12042 (#56) / Whole Map (#1265) | 1 | 180 | 0 | false |
| 5303 | Green Goddess Of Underground (#138) | D12042 (#56) / Whole Map (#1265) | 1 | 180 | 0 | false |
| 5304 | Stone Griffin (#133) | D12042 (#56) / Whole Map (#1265) | 1 | 80 | 2 | false |
| 5305 | Flame Griffin (#134) | D12042 (#56) / Whole Map (#1265) | 1 | 80 | 2 | false |
| 5306 | Vicious Mutant Captain (#137) | D12042 (#56) / Whole Map (#1265) | 1 | 130 | 0 | false |
| 5307 | Pink Goddess Of Underground (#136) | D1205 (#57) / Whole Map (#1270) | 1 | 750 | 0 | false |
| 5308 | Green Goddess Of Underground (#138) | D1205 (#57) / Whole Map (#1270) | 1 | 50 | 0 | false |
| 5309 | Stone Griffin (#133) | D1205 (#57) / Whole Map (#1270) | 1 | 300 | 2 | false |
| 5310 | Flame Griffin (#134) | D1205 (#57) / Whole Map (#1270) | 1 | 450 | 2 | false |
| 5311 | Vicious Mutant Captain (#137) | D1205 (#57) / Whole Map (#1270) | 1 | 600 | 0 | false |
| 5312 | Jinchon Warlord (#139) | D1205 (#57) / Whole Map (#1270) | 30 | 2 | 0 | false |
| 5313 | Jinchon Warlord (#139) | D12033 (#54) / Whole Map (#1245) | 60 | 1 | 0 | false |
| 5314 | Skeleton Axeman (#26) | D101 (#26) / Whole Map (#99) | 1 | 80 | 0 | false |
| 5315 | Skeleton Axeman (#26) | D102 (#31) / Whole Map (#369) | 1 | 100 | 0 | false |
| 5316 | Skeleton Axeman (#26) | D103 (#32) / Whole Map (#378) | 1 | 180 | 0 | false |
| 5317 | Skeleton Axeman (#26) | D121 (#59) / Whole Cave (#663) | 1 | 80 | 0 | false |
| 5318 | Skeleton Axeman (#26) | D122 (#60) / Whole Map (#670) | 1 | 100 | 0 | false |
| 5319 | Skeleton Axeman (#26) | D123 (#61) / Whole Map (#677) | 1 | 180 | 0 | false |
| 5320 | Skeleton Axeman (#26) | D111 (#39) / Whole Map (#519) | 1 | 80 | 0 | false |
| 5321 | Skeleton Axeman (#26) | D112 (#40) / Whole Map (#528) | 1 | 100 | 0 | false |
| 5322 | Skeleton Axeman (#26) | D113 (#41) / Whole Map (#535) | 1 | 180 | 0 | false |
| 5323 | Icy Goddess (#168) | D005 (#242) / Town Area (#820) | 5 | 8 | 0 | false |
| 5324 | Icy Goddess (#168) | 8 (#241) / Spawn Area - Town (#832) | 5 | 10 | 0 | false |
| 5325 | Black Palace Demon (#200) | D1305 (#67) / Boss Area (#1193) | 1 | 1 | 0 | true |
| 5326 | Stone Griffin (#133) | D1305 (#67) / Boss Area (#1193) | 5 | 7 | 1 | false |
| 5327 | Stone Griffin (#133) | D1305 (#67) / Whole Map (#1190) | 5 | 10 | 1 | false |
| 5328 | Flame Griffin (#134) | D1305 (#67) / Whole Map (#1190) | 5 | 10 | 1 | false |
| 5329 | Pink Goddess Of Black Palace (#130) | D1305 (#67) / Whole Map (#1190) | 5 | 10 | 0 | false |
| 5330 | Green Goddess Of Black Palace (#131) | D1305 (#67) / Whole Map (#1190) | 5 | 10 | 0 | false |
| 5331 | Mutant Captain (#132) | D1305 (#67) / Whole Map (#1190) | 5 | 10 | 0 | false |
| 5332 | Jinchon Devil (#199) | D1206 (#58) / Boss Area (#1278) | 1 | 1 | 0 | true |
| 5333 | Stone Griffin (#133) | D1206 (#58) / Boss Area (#1278) | 5 | 7 | 2 | false |
| 5334 | Stone Griffin (#133) | D1206 (#58) / Whole Map (#1275) | 5 | 10 | 2 | false |
| 5335 | Flame Griffin (#134) | D1206 (#58) / Whole Map (#1275) | 5 | 10 | 2 | false |
| 5336 | Pink Goddess Of Underground (#136) | D1206 (#58) / Whole Map (#1275) | 5 | 10 | 0 | false |
| 5337 | Green Goddess Of Underground (#138) | D1206 (#58) / Whole Map (#1275) | 5 | 10 | 0 | false |
| 5338 | Vicious Mutant Captain (#137) | D1206 (#58) / Whole Map (#1275) | 5 | 10 | 0 | false |
| 5340 | Evil Monkey (#84) | 11 (#291) / Open Area (#1471) | 15 | 400 | 0 | false |
| 5341 | Monkey (#83) | 11 (#291) / Open Area (#1471) | 15 | 400 | 0 | false |
| 5342 | Evil Elephant (#85) | 11 (#291) / Ridges (#1473) | 15 | 60 | 0 | false |
| 5343 | Evil Fanatic (#82) | 11 (#291) / Ridges (#1473) | 15 | 200 | 0 | false |
| 5344 | Cannibal Fanatic (#86) | 11 (#291) / Ridges (#1473) | 15 | 100 | 0 | false |
| 5345 | Crazed Warrior (#87) | 11 (#291) / Ridges (#1473) | 30 | 2 | 0 | false |
| 5346 | Brass Feral Warrior (#201) | D2401 (#294) / Whole Map (#1475) | 1 | 30 | 0 | false |
| 5347 | Obsidian Feral Warrior (#202) | D2401 (#294) / Whole Map (#1475) | 1 | 30 | 0 | false |
| 5348 | Sun Feral Warrior (#203) | D2401 (#294) / Whole Map (#1475) | 1 | 10 | 0 | false |
| 5349 | Moon Feral Warrior (#204) | D2401 (#294) / Whole Map (#1475) | 1 | 10 | 0 | false |
| 5351 | Flame Demon (#206) | D2401 (#294) / Whole Map (#1475) | 1 | 100 | 0 | false |
| 5352 | Ferocious Flame Demon (#209) | D2402 (#295) / Whole Map (#1480) | 1 | 100 | 0 | false |
| 5353 | Brass Feral Warrior (#201) | D2402 (#295) / Whole Map (#1480) | 1 | 125 | 0 | false |
| 5354 | Obsidian Feral Warrior (#202) | D2402 (#295) / Whole Map (#1480) | 1 | 150 | 0 | false |
| 5355 | Sun Feral Warrior (#203) | D2402 (#295) / Whole Map (#1480) | 1 | 100 | 0 | false |
| 5356 | Moon Feral Warrior (#204) | D2402 (#295) / Whole Map (#1480) | 1 | 100 | 0 | false |
| 5357 | Ox Feral General (#205) | D2402 (#295) / Whole Map (#1480) | 1 | 50 | 0 | false |
| 5358 | Flame Demon (#206) | D2402 (#295) / Whole Map (#1480) | 1 | 200 | 0 | false |
| 5359 | Ferocious Flame Demon (#209) | D2402 (#295) / Whole Map (#1480) | 1 | 200 | 0 | false |
| 5360 | Brass Feral Warrior (#201) | D2403 (#296) / Whole Map (#1485) | 1 | 155 | 0 | false |
| 5361 | Obsidian Feral Warrior (#202) | D2403 (#296) / Whole Map (#1485) | 1 | 155 | 0 | false |
| 5362 | Sun Feral Warrior (#203) | D2403 (#296) / Whole Map (#1485) | 1 | 155 | 0 | false |
| 5363 | Moon Feral Warrior (#204) | D2403 (#296) / Whole Map (#1485) | 1 | 155 | 0 | false |
| 5364 | Ox Feral General (#205) | D2403 (#296) / Whole Map (#1485) | 1 | 288 | 0 | false |
| 5365 | Flame Demon (#206) | D2403 (#296) / Whole Map (#1485) | 1 | 266 | 0 | false |
| 5366 | Ferocious Flame Demon (#209) | D2403 (#296) / Whole Map (#1485) | 1 | 277 | 0 | false |
| 5367 | Winged Horror (#207) | D2403 (#296) / Whole Map (#1485) | 160 | 1 | 0 | false |
| 5368 | Enraged Emperor Sa'Woo (#208) | D2403 (#296) / Whole Map (#1485) | 360 | 1 | 0 | false |
| 5369 | Icy Goddess (#168) | D005 (#242) / Mud Area (#830) | 1 | 8 | 0 | false |
| 5370 | Chicken (#8) | 10 (#259) / Town Area (#1495) | 1 | 50 | 0 | false |
| 5371 | Cow (#11) | 10 (#259) / Town Area (#1495) | 1 | 50 | 0 | false |
| 5372 | Pig (#9) | 10 (#259) / Town Area (#1495) | 1 | 50 | 0 | false |
| 5373 | Tiger Snake (#19) | 10 (#259) / Low Lands (#1532) | 1 | 600 | 0 | false |
| 5374 | Oma Hero (#23) | 10 (#259) / Low Lands (#1532) | 30 | 2 | 0 | false |
| 5375 | Oma Warlord (#210) | 10 (#259) / Cliffs (#1533) | 1 | 50 | 0 | false |
| 5376 | Oma Warlord (#210) | D2301 (#44) / Whole Map (#1512) | 1 | 64 | 0 | false |
| 5377 | Goru Spearman (#211) | D2301 (#44) / Whole Map (#1512) | 1 | 64 | 0 | false |
| 5378 | Goru Archer (#212) | D2301 (#44) / Whole Map (#1512) | 1 | 64 | 0 | false |
| 5379 | Oma Warlord (#210) | D2302 (#260) / Whole Map (#1517) | 1 | 144 | 0 | false |
| 5380 | Goru Spearman (#211) | D2302 (#260) / Whole Map (#1517) | 1 | 144 | 0 | false |
| 5381 | Goru Archer (#212) | D2302 (#260) / Whole Map (#1517) | 1 | 144 | 0 | false |
| 5382 | Goru General (#213) | D2302 (#260) / Whole Map (#1517) | 1 | 144 | 0 | false |
| 5384 | Goru Spearman (#211) | D2303 (#261) / Whole Map (#1522) | 1 | 355 | 0 | false |
| 5385 | Goru Archer (#212) | D2303 (#261) / Whole Map (#1522) | 1 | 355 | 0 | false |
| 5386 | Goru General (#213) | D2303 (#261) / Whole Map (#1522) | 1 | 355 | 0 | false |
| 5387 | Goru Spearman (#211) | D2304 (#262) / Whole Map (#1527) | 1 | 355 | 0 | false |
| 5388 | Goru Archer (#212) | D2304 (#262) / Whole Map (#1527) | 1 | 355 | 0 | false |
| 5389 | Goru General (#213) | D2304 (#262) / Whole Map (#1527) | 1 | 355 | 0 | false |
| 5390 | Enraged Arch Lich Taedu (#215) | D2304 (#262) / Whole Map (#1527) | 120 | 2 | 0 | false |
| 5391 | Apparition Archer (#141) | D1802 (#121) / Whole Map (#1539) | 1 | 45 | 0 | false |
| 5392 | Apparition Bladesman (#142) | D1802 (#121) / Whole Map (#1539) | 1 | 45 | 0 | false |
| 5393 | Apparition Soldier (#143) | D1802 (#121) / Whole Map (#1539) | 1 | 76 | 0 | false |
| 5394 | Escort Commander (#216) | D2201 (#219) / Whole Map (#1564) | 1 | 50 | 0 | false |
| 5395 | Fiery Dancer (#217) | D2201 (#219) / Whole Map (#1564) | 1 | 50 | 0 | false |
| 5396 | Emerald Dancer (#218) | D2201 (#219) / Whole Map (#1564) | 1 | 30 | 0 | false |
| 5397 | Escort Commander (#216) | D22021 (#273) / Whole Map (#1565) | 1 | 222 | 0 | false |
| 5398 | Fiery Dancer (#217) | D22021 (#273) / Whole Map (#1565) | 1 | 222 | 0 | false |
| 5399 | Emerald Dancer (#218) | D22021 (#273) / Whole Map (#1565) | 1 | 30 | 0 | false |
| 5400 | Escort Commander (#216) | D2204 (#277) / Whole Map (#1566) | 1 | 444 | 0 | false |
| 5401 | Fiery Dancer (#217) | D2204 (#277) / Whole Map (#1566) | 1 | 555 | 0 | false |
| 5402 | Emerald Dancer (#218) | D2204 (#277) / Whole Map (#1566) | 1 | 200 | 0 | false |
| 5403 | Escort Commander (#216) | D2205 (#278) / Whole Map (#1567) | 5 | 23 | 0 | false |
| 5404 | Fiery Dancer (#217) | D2205 (#278) / Whole Map (#1567) | 3 | 24 | 0 | false |
| 5405 | Emerald Dancer (#218) | D2205 (#278) / Whole Map (#1567) | 4 | 25 | 0 | false |
| 5406 | Queen Of Dawn (#219) | D2205 (#278) / Boss Area (#1568) | 166 | 1 | 0 | false |
| 5407 | Jinhwan Spirit (#225) | D006 (#332) / Whole Map (#1569) | 1 | 100 | 0 | false |
| 5408 | Jinhwan Guardian (#226) | D006 (#332) / Whole Map (#1569) | 1 | 100 | 0 | false |
| 5409 | Oyoung Beast (#221) | D006 (#332) / Whole Map (#1569) | 1 | 60 | 0 | false |
| 5410 | Oyoung General (#227) | D006 (#332) / Whole Map (#1569) | 1 | 60 | 0 | false |
| 5411 | Jinhwan Spirit (#225) | D007 (#333) / Whole Map (#1573) | 1 | 130 | 0 | false |
| 5412 | Jinhwan Guardian (#226) | D007 (#333) / Whole Map (#1573) | 1 | 130 | 0 | false |
| 5413 | Oyoung Beast (#221) | D007 (#333) / Whole Map (#1573) | 1 | 60 | 0 | false |
| 5414 | Oyoung General (#227) | D007 (#333) / Whole Map (#1573) | 1 | 60 | 0 | false |
| 5415 | Jinhwan Spirit (#225) | D2900 (#334) / Whole Map (#1578) | 1 | 20 | 0 | false |
| 5416 | Jinhwan Guardian (#226) | D2900 (#334) / Whole Map (#1578) | 1 | 20 | 0 | false |
| 5417 | Oyoung Beast (#221) | D2900 (#334) / Whole Map (#1578) | 1 | 10 | 0 | false |

### #5108 · Corpse Devourer (#44) / 4 (#8) / Spawn Ring 2 (#928)

| 字段 | 值 |
|---|---|
| Monster | Corpse Devourer (#44) |
| Region | 4 (#8) / Spawn Ring 2 (#928) |
| EventSpawn | false |
| Delay | 1 |
| Count | 300 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5109 · Visceral Worm (#45) / 4 (#8) / Spawn Ring 2 (#928)

| 字段 | 值 |
|---|---|
| Monster | Visceral Worm (#45) |
| Region | 4 (#8) / Spawn Ring 2 (#928) |
| EventSpawn | false |
| Delay | 1 |
| Count | 300 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5110 · Beetle (#43) / 4 (#8) / Spawn Ring 2 (#928)

| 字段 | 值 |
|---|---|
| Monster | Beetle (#43) |
| Region | 4 (#8) / Spawn Ring 2 (#928) |
| EventSpawn | false |
| Delay | 1 |
| Count | 300 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5111 · Spiked Beetle (#88) / 4 (#8) / Spawn Ring 2 (#928)

| 字段 | 值 |
|---|---|
| Monster | Spiked Beetle (#88) |
| Region | 4 (#8) / Spawn Ring 2 (#928) |
| EventSpawn | false |
| Delay | 30 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5112 · Numa Cavalry (#161) / D1501 (#74) / Whole Map (#929)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D1501 (#74) / Whole Map (#929) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5113 · Numa High Mage (#162) / D1501 (#74) / Whole Map (#929)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D1501 (#74) / Whole Map (#929) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5114 · Numa Stone Thrower (#163) / D1501 (#74) / Whole Map (#929)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D1501 (#74) / Whole Map (#929) |
| EventSpawn | false |
| Delay | 1 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5116 · Numa Royal Guard (#164) / D1501 (#74) / Whole Map (#929)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D1501 (#74) / Whole Map (#929) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5117 · Numa Armored Soldier (#165) / D1501 (#74) / Whole Map (#929)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D1501 (#74) / Whole Map (#929) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5118 · Numa Cavalry (#161) / D1502 (#75) / Whole Map (#940)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D1502 (#75) / Whole Map (#940) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5119 · Numa High Mage (#162) / D1502 (#75) / Whole Map (#940)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D1502 (#75) / Whole Map (#940) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5120 · Numa Stone Thrower (#163) / D1502 (#75) / Whole Map (#940)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D1502 (#75) / Whole Map (#940) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5121 · Numa Royal Guard (#164) / D1502 (#75) / Whole Map (#940)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D1502 (#75) / Whole Map (#940) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5122 · Numa Armored Soldier (#165) / D1502 (#75) / Whole Map (#940)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D1502 (#75) / Whole Map (#940) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5123 · Numa Cavalry (#161) / D1502 (#75) / Respawn Area (#951)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D1502 (#75) / Respawn Area (#951) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5124 · Numa High Mage (#162) / D1502 (#75) / Respawn Area (#951)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D1502 (#75) / Respawn Area (#951) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5125 · Numa Stone Thrower (#163) / D1502 (#75) / Respawn Area (#951)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D1502 (#75) / Respawn Area (#951) |
| EventSpawn | false |
| Delay | 15 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5126 · Numa Royal Guard (#164) / D1502 (#75) / Respawn Area (#951)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D1502 (#75) / Respawn Area (#951) |
| EventSpawn | false |
| Delay | 15 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5127 · Numa Armored Soldier (#165) / D1502 (#75) / Respawn Area (#951)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D1502 (#75) / Respawn Area (#951) |
| EventSpawn | false |
| Delay | 15 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5128 · Numa Cavalry (#161) / D15031 (#76) / Whole Map (#952)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D15031 (#76) / Whole Map (#952) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5129 · Numa High Mage (#162) / D15031 (#76) / Whole Map (#952)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D15031 (#76) / Whole Map (#952) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5130 · Numa Stone Thrower (#163) / D15031 (#76) / Whole Map (#952)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D15031 (#76) / Whole Map (#952) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5131 · Numa Royal Guard (#164) / D15031 (#76) / Whole Map (#952)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D15031 (#76) / Whole Map (#952) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5132 · Numa Armored Soldier (#165) / D15031 (#76) / Whole Map (#952)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D15031 (#76) / Whole Map (#952) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5133 · Numa Cavalry (#161) / D15032 (#77) / Whole Map (#957)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D15032 (#77) / Whole Map (#957) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5134 · Numa High Mage (#162) / D15032 (#77) / Whole Map (#957)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D15032 (#77) / Whole Map (#957) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5135 · Numa Stone Thrower (#163) / D15032 (#77) / Whole Map (#957)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D15032 (#77) / Whole Map (#957) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5136 · Numa Royal Guard (#164) / D15032 (#77) / Whole Map (#957)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D15032 (#77) / Whole Map (#957) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5137 · Numa Armored Soldier (#165) / D15032 (#77) / Whole Map (#957)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D15032 (#77) / Whole Map (#957) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5139 · Numa Cavalry (#161) / D15033 (#78) / Whole Map (#962)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D15033 (#78) / Whole Map (#962) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5140 · Numa High Mage (#162) / D15033 (#78) / Whole Map (#962)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D15033 (#78) / Whole Map (#962) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5141 · Numa Stone Thrower (#163) / D15033 (#78) / Whole Map (#962)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D15033 (#78) / Whole Map (#962) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5142 · Numa Royal Guard (#164) / D15033 (#78) / Whole Map (#962)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D15033 (#78) / Whole Map (#962) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5143 · Numa Armored Soldier (#165) / D15033 (#78) / Whole Map (#962)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D15033 (#78) / Whole Map (#962) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5144 · Numa Cavalry (#161) / D15034 (#79) / Whole Map (#967)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D15034 (#79) / Whole Map (#967) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5145 · Numa High Mage (#162) / D15034 (#79) / Whole Map (#967)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D15034 (#79) / Whole Map (#967) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5146 · Numa Stone Thrower (#163) / D15034 (#79) / Whole Map (#967)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D15034 (#79) / Whole Map (#967) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5147 · Numa Royal Guard (#164) / D15034 (#79) / Whole Map (#967)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D15034 (#79) / Whole Map (#967) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5148 · Numa Armored Soldier (#165) / D15034 (#79) / Whole Map (#967)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D15034 (#79) / Whole Map (#967) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5149 · Numa Cavalry (#161) / D1504 (#80) / Top Area (#972)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D1504 (#80) / Top Area (#972) |
| EventSpawn | false |
| Delay | 15 |
| Count | 35 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5150 · Numa High Mage (#162) / D1504 (#80) / Top Area (#972)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D1504 (#80) / Top Area (#972) |
| EventSpawn | false |
| Delay | 15 |
| Count | 35 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5151 · Numa Stone Thrower (#163) / D1504 (#80) / Top Area (#972)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D1504 (#80) / Top Area (#972) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5152 · Numa Royal Guard (#164) / D1504 (#80) / Top Area (#972)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D1504 (#80) / Top Area (#972) |
| EventSpawn | false |
| Delay | 15 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5153 · Numa Armored Soldier (#165) / D1504 (#80) / Top Area (#972)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D1504 (#80) / Top Area (#972) |
| EventSpawn | false |
| Delay | 15 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5155 · Numa Cavalry (#161) / D1504 (#80) / Left Area (#973)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D1504 (#80) / Left Area (#973) |
| EventSpawn | false |
| Delay | 30 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5156 · Numa High Mage (#162) / D1504 (#80) / Left Area (#973)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D1504 (#80) / Left Area (#973) |
| EventSpawn | false |
| Delay | 30 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5157 · Numa Stone Thrower (#163) / D1504 (#80) / Left Area (#973)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D1504 (#80) / Left Area (#973) |
| EventSpawn | false |
| Delay | 30 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5158 · Numa Royal Guard (#164) / D1504 (#80) / Left Area (#973)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D1504 (#80) / Left Area (#973) |
| EventSpawn | false |
| Delay | 30 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5159 · Numa Armored Soldier (#165) / D1504 (#80) / Left Area (#973)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D1504 (#80) / Left Area (#973) |
| EventSpawn | false |
| Delay | 30 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5161 · Numa Cavalry (#161) / D1504 (#80) / Right Area (#974)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D1504 (#80) / Right Area (#974) |
| EventSpawn | false |
| Delay | 30 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5162 · Numa High Mage (#162) / D1504 (#80) / Right Area (#974)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D1504 (#80) / Right Area (#974) |
| EventSpawn | false |
| Delay | 30 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5163 · Numa Stone Thrower (#163) / D1504 (#80) / Right Area (#974)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D1504 (#80) / Right Area (#974) |
| EventSpawn | false |
| Delay | 30 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5164 · Numa Royal Guard (#164) / D1504 (#80) / Right Area (#974)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D1504 (#80) / Right Area (#974) |
| EventSpawn | false |
| Delay | 30 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5165 · Numa Armored Soldier (#165) / D1504 (#80) / Right Area (#974)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D1504 (#80) / Right Area (#974) |
| EventSpawn | false |
| Delay | 30 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5166 · Numa Cavalry (#161) / D1505 (#81) / Row 1 Area (#993)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D1505 (#81) / Row 1 Area (#993) |
| EventSpawn | false |
| Delay | 15 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5167 · Numa High Mage (#162) / D1505 (#81) / Row 1 Area (#993)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D1505 (#81) / Row 1 Area (#993) |
| EventSpawn | false |
| Delay | 15 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5168 · Numa Stone Thrower (#163) / D1505 (#81) / Row 1 Area (#993)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D1505 (#81) / Row 1 Area (#993) |
| EventSpawn | false |
| Delay | 15 |
| Count | 5 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5169 · Numa Royal Guard (#164) / D1505 (#81) / Row 1 Area (#993)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D1505 (#81) / Row 1 Area (#993) |
| EventSpawn | false |
| Delay | 15 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5170 · Numa Armored Soldier (#165) / D1505 (#81) / Row 1 Area (#993)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D1505 (#81) / Row 1 Area (#993) |
| EventSpawn | false |
| Delay | 15 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5171 · Numa Cavalry (#161) / D1505 (#81) / Row 2 Area (#994)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D1505 (#81) / Row 2 Area (#994) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5172 · Numa High Mage (#162) / D1505 (#81) / Row 2 Area (#994)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D1505 (#81) / Row 2 Area (#994) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5173 · Numa Stone Thrower (#163) / D1505 (#81) / Row 2 Area (#994)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D1505 (#81) / Row 2 Area (#994) |
| EventSpawn | false |
| Delay | 15 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5174 · Numa Royal Guard (#164) / D1505 (#81) / Row 2 Area (#994)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D1505 (#81) / Row 2 Area (#994) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5175 · Numa Armored Soldier (#165) / D1505 (#81) / Row 2 Area (#994)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D1505 (#81) / Row 2 Area (#994) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5176 · Numa Cavalry (#161) / D1505 (#81) / Row 3 Area (#995)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D1505 (#81) / Row 3 Area (#995) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5177 · Numa High Mage (#162) / D1505 (#81) / Row 3 Area (#995)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D1505 (#81) / Row 3 Area (#995) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5178 · Numa Stone Thrower (#163) / D1505 (#81) / Row 3 Area (#995)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D1505 (#81) / Row 3 Area (#995) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5179 · Numa Royal Guard (#164) / D1505 (#81) / Row 3 Area (#995)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D1505 (#81) / Row 3 Area (#995) |
| EventSpawn | false |
| Delay | 15 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5180 · Numa Armored Soldier (#165) / D1505 (#81) / Row 3 Area (#995)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D1505 (#81) / Row 3 Area (#995) |
| EventSpawn | false |
| Delay | 15 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5181 · Numa Cavalry (#161) / D1505 (#81) / Row 4 Area (#996)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D1505 (#81) / Row 4 Area (#996) |
| EventSpawn | false |
| Delay | 15 |
| Count | 25 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5182 · Numa High Mage (#162) / D1505 (#81) / Row 4 Area (#996)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D1505 (#81) / Row 4 Area (#996) |
| EventSpawn | false |
| Delay | 15 |
| Count | 25 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5183 · Numa Stone Thrower (#163) / D1505 (#81) / Row 4 Area (#996)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D1505 (#81) / Row 4 Area (#996) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5184 · Numa Royal Guard (#164) / D1505 (#81) / Row 4 Area (#996)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D1505 (#81) / Row 4 Area (#996) |
| EventSpawn | false |
| Delay | 15 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5185 · Numa Armored Soldier (#165) / D1505 (#81) / Row 4 Area (#996)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D1505 (#81) / Row 4 Area (#996) |
| EventSpawn | false |
| Delay | 15 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5186 · Numa Armored Soldier (#165) / D1505 (#81) / Bottom Left Area (#1007)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D1505 (#81) / Bottom Left Area (#1007) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5187 · Numa Armored Soldier (#165) / D1505 (#81) / Bottom Right Area (#1008)

| 字段 | 值 |
|---|---|
| Monster | Numa Armored Soldier (#165) |
| Region | D1505 (#81) / Bottom Right Area (#1008) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5188 · Numa Royal Guard (#164) / D1505 (#81) / Bottom Left Area (#1007)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D1505 (#81) / Bottom Left Area (#1007) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5189 · Numa Royal Guard (#164) / D1505 (#81) / Bottom Right Area (#1008)

| 字段 | 值 |
|---|---|
| Monster | Numa Royal Guard (#164) |
| Region | D1505 (#81) / Bottom Right Area (#1008) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5190 · Numa Cavalry (#161) / D1505 (#81) / Bottom Left Area (#1007)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D1505 (#81) / Bottom Left Area (#1007) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5191 · Numa Cavalry (#161) / D1505 (#81) / Bottom Right Area (#1008)

| 字段 | 值 |
|---|---|
| Monster | Numa Cavalry (#161) |
| Region | D1505 (#81) / Bottom Right Area (#1008) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5192 · Numa Stone Thrower (#163) / D1505 (#81) / Bottom Left Area (#1007)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D1505 (#81) / Bottom Left Area (#1007) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5193 · Numa Stone Thrower (#163) / D1505 (#81) / Bottom Right Area (#1008)

| 字段 | 值 |
|---|---|
| Monster | Numa Stone Thrower (#163) |
| Region | D1505 (#81) / Bottom Right Area (#1008) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5194 · Numa High Mage (#162) / D1505 (#81) / Bottom Left Area (#1007)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D1505 (#81) / Bottom Left Area (#1007) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5195 · Numa High Mage (#162) / D1505 (#81) / Bottom Right Area (#1008)

| 字段 | 值 |
|---|---|
| Monster | Numa High Mage (#162) |
| Region | D1505 (#81) / Bottom Right Area (#1008) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5196 · Numa Assault Captain (#166) / D1505 (#81) / Bottom Left Area (#1007)

| 字段 | 值 |
|---|---|
| Monster | Numa Assault Captain (#166) |
| Region | D1505 (#81) / Bottom Left Area (#1007) |
| EventSpawn | false |
| Delay | 30 |
| Count | 1 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5197 · Numa Assault Captain (#166) / D1505 (#81) / Bottom Right Area (#1008)

| 字段 | 值 |
|---|---|
| Monster | Numa Assault Captain (#166) |
| Region | D1505 (#81) / Bottom Right Area (#1008) |
| EventSpawn | false |
| Delay | 30 |
| Count | 1 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5198 · Decaying Ghoul (#58) / 12 (#292) / Spawn Area (#1125)

| 字段 | 值 |
|---|---|
| Monster | Decaying Ghoul (#58) |
| Region | 12 (#292) / Spawn Area (#1125) |
| EventSpawn | false |
| Delay | 1 |
| Count | 450 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5199 · Rotting Ghoul (#57) / 12 (#292) / Spawn Area (#1125)

| 字段 | 值 |
|---|---|
| Monster | Rotting Ghoul (#57) |
| Region | 12 (#292) / Spawn Area (#1125) |
| EventSpawn | false |
| Delay | 1 |
| Count | 450 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5200 · Blood Thristy Zombie (#60) / 12 (#292) / Spawn Area (#1125)

| 字段 | 值 |
|---|---|
| Monster | Blood Thristy Zombie (#60) |
| Region | 12 (#292) / Spawn Area (#1125) |
| EventSpawn | false |
| Delay | 60 |
| Count | 4 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5201 · Bloody Armed Beetle (#179) / D2501 (#298) / Whole Map (#1128)

| 字段 | 值 |
|---|---|
| Monster | Bloody Armed Beetle (#179) |
| Region | D2501 (#298) / Whole Map (#1128) |
| EventSpawn | false |
| Delay | 1 |
| Count | 75 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5202 · Earwig King (#181) / D2501 (#298) / Whole Map (#1128)

| 字段 | 值 |
|---|---|
| Monster | Earwig King (#181) |
| Region | D2501 (#298) / Whole Map (#1128) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5203 · Bloody Armed Beetle (#179) / D2502 (#299) / Whole Map (#1133)

| 字段 | 值 |
|---|---|
| Monster | Bloody Armed Beetle (#179) |
| Region | D2502 (#299) / Whole Map (#1133) |
| EventSpawn | false |
| Delay | 1 |
| Count | 110 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5204 · Earwig King (#181) / D2502 (#299) / Whole Map (#1133)

| 字段 | 值 |
|---|---|
| Monster | Earwig King (#181) |
| Region | D2502 (#299) / Whole Map (#1133) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5205 · Bloody Armed Beetle (#179) / D2503 (#300) / Whole Map (#1138)

| 字段 | 值 |
|---|---|
| Monster | Bloody Armed Beetle (#179) |
| Region | D2503 (#300) / Whole Map (#1138) |
| EventSpawn | false |
| Delay | 1 |
| Count | 140 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5206 · Earwig King (#181) / D2503 (#300) / Whole Map (#1138)

| 字段 | 值 |
|---|---|
| Monster | Earwig King (#181) |
| Region | D2503 (#300) / Whole Map (#1138) |
| EventSpawn | false |
| Delay | 1 |
| Count | 110 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5207 · Enraged Lord Ji'Nae (#184) / D2503 (#300) / Whole Map (#1138)

| 字段 | 值 |
|---|---|
| Monster | Enraged Lord Ji'Nae (#184) |
| Region | D2503 (#300) / Whole Map (#1138) |
| EventSpawn | false |
| Delay | 150 |
| Count | 1 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5208 · Bloody Armed Beetle (#179) / D2503 (#300) / Whole Map (#1138)

| 字段 | 值 |
|---|---|
| Monster | Bloody Armed Beetle (#179) |
| Region | D2503 (#300) / Whole Map (#1138) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5209 · Earwig King (#181) / D2503 (#300) / Whole Map (#1138)

| 字段 | 值 |
|---|---|
| Monster | Earwig King (#181) |
| Region | D2503 (#300) / Whole Map (#1138) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5210 · Banyo Soldier (#185) / 13 (#293) / Whole Map (#1144)

| 字段 | 值 |
|---|---|
| Monster | Banyo Soldier (#185) |
| Region | 13 (#293) / Whole Map (#1144) |
| EventSpawn | false |
| Delay | 1 |
| Count | 125 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5211 · Banyo Warrior (#186) / 13 (#293) / Whole Map (#1144)

| 字段 | 值 |
|---|---|
| Monster | Banyo Warrior (#186) |
| Region | 13 (#293) / Whole Map (#1144) |
| EventSpawn | false |
| Delay | 1 |
| Count | 48 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5214 · Banyo Soldier (#185) / D2601 (#301) / Spawn Area (#1149)

| 字段 | 值 |
|---|---|
| Monster | Banyo Soldier (#185) |
| Region | D2601 (#301) / Spawn Area (#1149) |
| EventSpawn | false |
| Delay | 1 |
| Count | 555 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5215 · Banyo Warrior (#186) / D2601 (#301) / Spawn Area (#1149)

| 字段 | 值 |
|---|---|
| Monster | Banyo Warrior (#186) |
| Region | D2601 (#301) / Spawn Area (#1149) |
| EventSpawn | false |
| Delay | 1 |
| Count | 333 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5216 · Banyo Captain (#187) / D2601 (#301) / Spawn Area (#1149)

| 字段 | 值 |
|---|---|
| Monster | Banyo Captain (#187) |
| Region | D2601 (#301) / Spawn Area (#1149) |
| EventSpawn | false |
| Delay | 1 |
| Count | 600 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5217 · Banyo Lord Guzak (#188) / D2601 (#301) / Spawn Area (#1149)

| 字段 | 值 |
|---|---|
| Monster | Banyo Lord Guzak (#188) |
| Region | D2601 (#301) / Spawn Area (#1149) |
| EventSpawn | false |
| Delay | 210 |
| Count | 1 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5218 · Pink Goddess Of Black Palace (#130) / D1301 (#62) / Whole Map (#1159)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Black Palace (#130) |
| Region | D1301 (#62) / Whole Map (#1159) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5219 · Green Goddess Of Black Palace (#131) / D1301 (#62) / Whole Map (#1159)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Black Palace (#131) |
| Region | D1301 (#62) / Whole Map (#1159) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5220 · Stone Griffin (#133) / D1301 (#62) / Whole Map (#1159)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D1301 (#62) / Whole Map (#1159) |
| EventSpawn | false |
| Delay | 1 |
| Count | 30 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5221 · Flame Griffin (#134) / D1301 (#62) / Whole Map (#1159)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D1301 (#62) / Whole Map (#1159) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5222 · Pink Goddess Of Black Palace (#130) / D13021 (#63) / Whole Map (#1168)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Black Palace (#130) |
| Region | D13021 (#63) / Whole Map (#1168) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5223 · Green Goddess Of Black Palace (#131) / D13021 (#63) / Whole Map (#1168)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Black Palace (#131) |
| Region | D13021 (#63) / Whole Map (#1168) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5224 · Stone Griffin (#133) / D13021 (#63) / Whole Map (#1168)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D13021 (#63) / Whole Map (#1168) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5225 · Flame Griffin (#134) / D13021 (#63) / Whole Map (#1168)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D13021 (#63) / Whole Map (#1168) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5226 · Pink Goddess Of Black Palace (#130) / D13022 (#64) / Whole Map (#1173)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Black Palace (#130) |
| Region | D13022 (#64) / Whole Map (#1173) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5227 · Green Goddess Of Black Palace (#131) / D13022 (#64) / Whole Map (#1173)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Black Palace (#131) |
| Region | D13022 (#64) / Whole Map (#1173) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5228 · Stone Griffin (#133) / D13022 (#64) / Whole Map (#1173)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D13022 (#64) / Whole Map (#1173) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5229 · Flame Griffin (#134) / D13022 (#64) / Whole Map (#1173)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D13022 (#64) / Whole Map (#1173) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5230 · Pink Goddess Of Black Palace (#130) / D1303 (#65) / Whole Map (#1178)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Black Palace (#130) |
| Region | D1303 (#65) / Whole Map (#1178) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5231 · Green Goddess Of Black Palace (#131) / D1303 (#65) / Whole Map (#1178)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Black Palace (#131) |
| Region | D1303 (#65) / Whole Map (#1178) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5232 · Stone Griffin (#133) / D1303 (#65) / Whole Map (#1178)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D1303 (#65) / Whole Map (#1178) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5233 · Flame Griffin (#134) / D1303 (#65) / Whole Map (#1178)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D1303 (#65) / Whole Map (#1178) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5234 · Mutant Captain (#132) / D1303 (#65) / Whole Map (#1178)

| 字段 | 值 |
|---|---|
| Monster | Mutant Captain (#132) |
| Region | D1303 (#65) / Whole Map (#1178) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5235 · Pink Goddess Of Black Palace (#130) / D1304 (#66) / Whole Map (#1185)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Black Palace (#130) |
| Region | D1304 (#66) / Whole Map (#1185) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5236 · Green Goddess Of Black Palace (#131) / D1304 (#66) / Whole Map (#1185)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Black Palace (#131) |
| Region | D1304 (#66) / Whole Map (#1185) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5237 · Stone Griffin (#133) / D1304 (#66) / Whole Map (#1185)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D1304 (#66) / Whole Map (#1185) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5238 · Flame Griffin (#134) / D1304 (#66) / Whole Map (#1185)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D1304 (#66) / Whole Map (#1185) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5239 · Mutant Captain (#132) / D1304 (#66) / Whole Map (#1185)

| 字段 | 值 |
|---|---|
| Monster | Mutant Captain (#132) |
| Region | D1304 (#66) / Whole Map (#1185) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5240 · Black Palace Warlord (#135) / D1304 (#66) / Whole Map (#1185)

| 字段 | 值 |
|---|---|
| Monster | Black Palace Warlord (#135) |
| Region | D1304 (#66) / Whole Map (#1185) |
| EventSpawn | false |
| Delay | 30 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5241 · Pink Goddess Of Underground (#136) / D1200 (#42) / Whole Map (#1194)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D1200 (#42) / Whole Map (#1194) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5242 · Green Goddess Of Underground (#138) / D1200 (#42) / Whole Map (#1194)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D1200 (#42) / Whole Map (#1194) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5243 · Stone Griffin (#133) / D1200 (#42) / Whole Map (#1194)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D1200 (#42) / Whole Map (#1194) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5244 · Flame Griffin (#134) / D1200 (#42) / Whole Map (#1194)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D1200 (#42) / Whole Map (#1194) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5245 · Pink Goddess Of Underground (#136) / D12011 (#43) / Whole Map (#1207)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D12011 (#43) / Whole Map (#1207) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5246 · Green Goddess Of Underground (#138) / D12011 (#43) / Whole Map (#1207)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D12011 (#43) / Whole Map (#1207) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5247 · Stone Griffin (#133) / D12011 (#43) / Whole Map (#1207)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D12011 (#43) / Whole Map (#1207) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5248 · Flame Griffin (#134) / D12011 (#43) / Whole Map (#1207)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D12011 (#43) / Whole Map (#1207) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5249 · Pink Goddess Of Underground (#136) / D12012 (#45) / Whole Map (#1212)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D12012 (#45) / Whole Map (#1212) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5250 · Green Goddess Of Underground (#138) / D12012 (#45) / Whole Map (#1212)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D12012 (#45) / Whole Map (#1212) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5251 · Stone Griffin (#133) / D12012 (#45) / Whole Map (#1212)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D12012 (#45) / Whole Map (#1212) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5252 · Flame Griffin (#134) / D12012 (#45) / Whole Map (#1212)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D12012 (#45) / Whole Map (#1212) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5254 · Pink Goddess Of Underground (#136) / D12013 (#46) / Whole Map (#1215)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D12013 (#46) / Whole Map (#1215) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5255 · Green Goddess Of Underground (#138) / D12013 (#46) / Whole Map (#1215)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D12013 (#46) / Whole Map (#1215) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5256 · Stone Griffin (#133) / D12013 (#46) / Whole Map (#1215)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D12013 (#46) / Whole Map (#1215) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5257 · Flame Griffin (#134) / D12013 (#46) / Whole Map (#1215)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D12013 (#46) / Whole Map (#1215) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5258 · Pink Goddess Of Underground (#136) / D12014 (#47) / Whole Map (#1220)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D12014 (#47) / Whole Map (#1220) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5259 · Green Goddess Of Underground (#138) / D12014 (#47) / Whole Map (#1220)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D12014 (#47) / Whole Map (#1220) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5260 · Stone Griffin (#133) / D12014 (#47) / Whole Map (#1220)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D12014 (#47) / Whole Map (#1220) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5261 · Flame Griffin (#134) / D12014 (#47) / Whole Map (#1220)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D12014 (#47) / Whole Map (#1220) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5262 · Pink Goddess Of Underground (#136) / D12021 (#48) / Whole Map (#1225)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D12021 (#48) / Whole Map (#1225) |
| EventSpawn | false |
| Delay | 1 |
| Count | 130 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5263 · Green Goddess Of Underground (#138) / D12021 (#48) / Whole Map (#1225)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D12021 (#48) / Whole Map (#1225) |
| EventSpawn | false |
| Delay | 1 |
| Count | 130 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5264 · Stone Griffin (#133) / D12021 (#48) / Whole Map (#1225)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D12021 (#48) / Whole Map (#1225) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5265 · Flame Griffin (#134) / D12021 (#48) / Whole Map (#1225)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D12021 (#48) / Whole Map (#1225) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5266 · Vicious Mutant Captain (#137) / D12021 (#48) / Whole Map (#1225)

| 字段 | 值 |
|---|---|
| Monster | Vicious Mutant Captain (#137) |
| Region | D12021 (#48) / Whole Map (#1225) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5267 · Pink Goddess Of Underground (#136) / D12022 (#49) / Whole Map (#1230)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D12022 (#49) / Whole Map (#1230) |
| EventSpawn | false |
| Delay | 1 |
| Count | 130 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5268 · Green Goddess Of Underground (#138) / D12022 (#49) / Whole Map (#1230)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D12022 (#49) / Whole Map (#1230) |
| EventSpawn | false |
| Delay | 1 |
| Count | 130 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5269 · Stone Griffin (#133) / D12022 (#49) / Whole Map (#1230)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D12022 (#49) / Whole Map (#1230) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5270 · Flame Griffin (#134) / D12022 (#49) / Whole Map (#1230)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D12022 (#49) / Whole Map (#1230) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5271 · Vicious Mutant Captain (#137) / D12022 (#49) / Whole Map (#1230)

| 字段 | 值 |
|---|---|
| Monster | Vicious Mutant Captain (#137) |
| Region | D12022 (#49) / Whole Map (#1230) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5272 · Pink Goddess Of Underground (#136) / D12023 (#50) / Whole Map (#1235)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D12023 (#50) / Whole Map (#1235) |
| EventSpawn | false |
| Delay | 1 |
| Count | 160 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5273 · Green Goddess Of Underground (#138) / D12023 (#50) / Whole Map (#1235)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D12023 (#50) / Whole Map (#1235) |
| EventSpawn | false |
| Delay | 1 |
| Count | 160 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5274 · Stone Griffin (#133) / D12023 (#50) / Whole Map (#1235)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D12023 (#50) / Whole Map (#1235) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5275 · Flame Griffin (#134) / D12023 (#50) / Whole Map (#1235)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D12023 (#50) / Whole Map (#1235) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5276 · Vicious Mutant Captain (#137) / D12023 (#50) / Whole Map (#1235)

| 字段 | 值 |
|---|---|
| Monster | Vicious Mutant Captain (#137) |
| Region | D12023 (#50) / Whole Map (#1235) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5277 · Pink Goddess Of Underground (#136) / D12024 (#51) / Whole Map (#1240)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D12024 (#51) / Whole Map (#1240) |
| EventSpawn | false |
| Delay | 1 |
| Count | 130 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5278 · Green Goddess Of Underground (#138) / D12024 (#51) / Whole Map (#1240)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D12024 (#51) / Whole Map (#1240) |
| EventSpawn | false |
| Delay | 1 |
| Count | 130 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5279 · Stone Griffin (#133) / D12024 (#51) / Whole Map (#1240)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D12024 (#51) / Whole Map (#1240) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5280 · Flame Griffin (#134) / D12024 (#51) / Whole Map (#1240)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D12024 (#51) / Whole Map (#1240) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5281 · Vicious Mutant Captain (#137) / D12024 (#51) / Whole Map (#1240)

| 字段 | 值 |
|---|---|
| Monster | Vicious Mutant Captain (#137) |
| Region | D12024 (#51) / Whole Map (#1240) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5282 · Pink Goddess Of Underground (#136) / D12033 (#54) / Whole Map (#1245)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D12033 (#54) / Whole Map (#1245) |
| EventSpawn | false |
| Delay | 1 |
| Count | 180 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5283 · Green Goddess Of Underground (#138) / D12033 (#54) / Whole Map (#1245)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D12033 (#54) / Whole Map (#1245) |
| EventSpawn | false |
| Delay | 1 |
| Count | 180 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5284 · Stone Griffin (#133) / D12033 (#54) / Whole Map (#1245)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D12033 (#54) / Whole Map (#1245) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5285 · Flame Griffin (#134) / D12033 (#54) / Whole Map (#1245)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D12033 (#54) / Whole Map (#1245) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5286 · Vicious Mutant Captain (#137) / D12033 (#54) / Whole Map (#1245)

| 字段 | 值 |
|---|---|
| Monster | Vicious Mutant Captain (#137) |
| Region | D12033 (#54) / Whole Map (#1245) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5287 · Pink Goddess Of Underground (#136) / D12031 (#52) / Whole Map (#1248)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D12031 (#52) / Whole Map (#1248) |
| EventSpawn | false |
| Delay | 1 |
| Count | 130 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5288 · Green Goddess Of Underground (#138) / D12031 (#52) / Whole Map (#1248)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D12031 (#52) / Whole Map (#1248) |
| EventSpawn | false |
| Delay | 1 |
| Count | 130 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5289 · Stone Griffin (#133) / D12031 (#52) / Whole Map (#1248)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D12031 (#52) / Whole Map (#1248) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5290 · Flame Griffin (#134) / D12031 (#52) / Whole Map (#1248)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D12031 (#52) / Whole Map (#1248) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5291 · Vicious Mutant Captain (#137) / D12031 (#52) / Whole Map (#1248)

| 字段 | 值 |
|---|---|
| Monster | Vicious Mutant Captain (#137) |
| Region | D12031 (#52) / Whole Map (#1248) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5292 · Pink Goddess Of Underground (#136) / D12032 (#53) / Whole Map (#1253)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D12032 (#53) / Whole Map (#1253) |
| EventSpawn | false |
| Delay | 1 |
| Count | 160 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5293 · Green Goddess Of Underground (#138) / D12032 (#53) / Whole Map (#1253)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D12032 (#53) / Whole Map (#1253) |
| EventSpawn | false |
| Delay | 1 |
| Count | 160 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5294 · Stone Griffin (#133) / D12032 (#53) / Whole Map (#1253)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D12032 (#53) / Whole Map (#1253) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5295 · Flame Griffin (#134) / D12032 (#53) / Whole Map (#1253)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D12032 (#53) / Whole Map (#1253) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5296 · Vicious Mutant Captain (#137) / D12032 (#53) / Whole Map (#1253)

| 字段 | 值 |
|---|---|
| Monster | Vicious Mutant Captain (#137) |
| Region | D12032 (#53) / Whole Map (#1253) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5297 · Pink Goddess Of Underground (#136) / D12041 (#55) / Whole Map (#1260)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D12041 (#55) / Whole Map (#1260) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5298 · Green Goddess Of Underground (#138) / D12041 (#55) / Whole Map (#1260)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D12041 (#55) / Whole Map (#1260) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5299 · Stone Griffin (#133) / D12041 (#55) / Whole Map (#1260)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D12041 (#55) / Whole Map (#1260) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5300 · Flame Griffin (#134) / D12041 (#55) / Whole Map (#1260)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D12041 (#55) / Whole Map (#1260) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5301 · Vicious Mutant Captain (#137) / D12041 (#55) / Whole Map (#1260)

| 字段 | 值 |
|---|---|
| Monster | Vicious Mutant Captain (#137) |
| Region | D12041 (#55) / Whole Map (#1260) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5302 · Pink Goddess Of Underground (#136) / D12042 (#56) / Whole Map (#1265)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D12042 (#56) / Whole Map (#1265) |
| EventSpawn | false |
| Delay | 1 |
| Count | 180 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5303 · Green Goddess Of Underground (#138) / D12042 (#56) / Whole Map (#1265)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D12042 (#56) / Whole Map (#1265) |
| EventSpawn | false |
| Delay | 1 |
| Count | 180 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5304 · Stone Griffin (#133) / D12042 (#56) / Whole Map (#1265)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D12042 (#56) / Whole Map (#1265) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5305 · Flame Griffin (#134) / D12042 (#56) / Whole Map (#1265)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D12042 (#56) / Whole Map (#1265) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5306 · Vicious Mutant Captain (#137) / D12042 (#56) / Whole Map (#1265)

| 字段 | 值 |
|---|---|
| Monster | Vicious Mutant Captain (#137) |
| Region | D12042 (#56) / Whole Map (#1265) |
| EventSpawn | false |
| Delay | 1 |
| Count | 130 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5307 · Pink Goddess Of Underground (#136) / D1205 (#57) / Whole Map (#1270)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D1205 (#57) / Whole Map (#1270) |
| EventSpawn | false |
| Delay | 1 |
| Count | 750 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5308 · Green Goddess Of Underground (#138) / D1205 (#57) / Whole Map (#1270)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D1205 (#57) / Whole Map (#1270) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5309 · Stone Griffin (#133) / D1205 (#57) / Whole Map (#1270)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D1205 (#57) / Whole Map (#1270) |
| EventSpawn | false |
| Delay | 1 |
| Count | 300 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5310 · Flame Griffin (#134) / D1205 (#57) / Whole Map (#1270)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D1205 (#57) / Whole Map (#1270) |
| EventSpawn | false |
| Delay | 1 |
| Count | 450 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5311 · Vicious Mutant Captain (#137) / D1205 (#57) / Whole Map (#1270)

| 字段 | 值 |
|---|---|
| Monster | Vicious Mutant Captain (#137) |
| Region | D1205 (#57) / Whole Map (#1270) |
| EventSpawn | false |
| Delay | 1 |
| Count | 600 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5312 · Jinchon Warlord (#139) / D1205 (#57) / Whole Map (#1270)

| 字段 | 值 |
|---|---|
| Monster | Jinchon Warlord (#139) |
| Region | D1205 (#57) / Whole Map (#1270) |
| EventSpawn | false |
| Delay | 30 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5313 · Jinchon Warlord (#139) / D12033 (#54) / Whole Map (#1245)

| 字段 | 值 |
|---|---|
| Monster | Jinchon Warlord (#139) |
| Region | D12033 (#54) / Whole Map (#1245) |
| EventSpawn | false |
| Delay | 60 |
| Count | 1 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5314 · Skeleton Axeman (#26) / D101 (#26) / Whole Map (#99)

| 字段 | 值 |
|---|---|
| Monster | Skeleton Axeman (#26) |
| Region | D101 (#26) / Whole Map (#99) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5315 · Skeleton Axeman (#26) / D102 (#31) / Whole Map (#369)

| 字段 | 值 |
|---|---|
| Monster | Skeleton Axeman (#26) |
| Region | D102 (#31) / Whole Map (#369) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5316 · Skeleton Axeman (#26) / D103 (#32) / Whole Map (#378)

| 字段 | 值 |
|---|---|
| Monster | Skeleton Axeman (#26) |
| Region | D103 (#32) / Whole Map (#378) |
| EventSpawn | false |
| Delay | 1 |
| Count | 180 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5317 · Skeleton Axeman (#26) / D121 (#59) / Whole Cave (#663)

| 字段 | 值 |
|---|---|
| Monster | Skeleton Axeman (#26) |
| Region | D121 (#59) / Whole Cave (#663) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5318 · Skeleton Axeman (#26) / D122 (#60) / Whole Map (#670)

| 字段 | 值 |
|---|---|
| Monster | Skeleton Axeman (#26) |
| Region | D122 (#60) / Whole Map (#670) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5319 · Skeleton Axeman (#26) / D123 (#61) / Whole Map (#677)

| 字段 | 值 |
|---|---|
| Monster | Skeleton Axeman (#26) |
| Region | D123 (#61) / Whole Map (#677) |
| EventSpawn | false |
| Delay | 1 |
| Count | 180 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5320 · Skeleton Axeman (#26) / D111 (#39) / Whole Map (#519)

| 字段 | 值 |
|---|---|
| Monster | Skeleton Axeman (#26) |
| Region | D111 (#39) / Whole Map (#519) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5321 · Skeleton Axeman (#26) / D112 (#40) / Whole Map (#528)

| 字段 | 值 |
|---|---|
| Monster | Skeleton Axeman (#26) |
| Region | D112 (#40) / Whole Map (#528) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5322 · Skeleton Axeman (#26) / D113 (#41) / Whole Map (#535)

| 字段 | 值 |
|---|---|
| Monster | Skeleton Axeman (#26) |
| Region | D113 (#41) / Whole Map (#535) |
| EventSpawn | false |
| Delay | 1 |
| Count | 180 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5323 · Icy Goddess (#168) / D005 (#242) / Town Area (#820)

| 字段 | 值 |
|---|---|
| Monster | Icy Goddess (#168) |
| Region | D005 (#242) / Town Area (#820) |
| EventSpawn | false |
| Delay | 5 |
| Count | 8 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5324 · Icy Goddess (#168) / 8 (#241) / Spawn Area - Town (#832)

| 字段 | 值 |
|---|---|
| Monster | Icy Goddess (#168) |
| Region | 8 (#241) / Spawn Area - Town (#832) |
| EventSpawn | false |
| Delay | 5 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5325 · Black Palace Demon (#200) / D1305 (#67) / Boss Area (#1193)

| 字段 | 值 |
|---|---|
| Monster | Black Palace Demon (#200) |
| Region | D1305 (#67) / Boss Area (#1193) |
| EventSpawn | true |
| Delay | 1 |
| Count | 1 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5326 · Stone Griffin (#133) / D1305 (#67) / Boss Area (#1193)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D1305 (#67) / Boss Area (#1193) |
| EventSpawn | false |
| Delay | 5 |
| Count | 7 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5327 · Stone Griffin (#133) / D1305 (#67) / Whole Map (#1190)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D1305 (#67) / Whole Map (#1190) |
| EventSpawn | false |
| Delay | 5 |
| Count | 10 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5328 · Flame Griffin (#134) / D1305 (#67) / Whole Map (#1190)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D1305 (#67) / Whole Map (#1190) |
| EventSpawn | false |
| Delay | 5 |
| Count | 10 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5329 · Pink Goddess Of Black Palace (#130) / D1305 (#67) / Whole Map (#1190)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Black Palace (#130) |
| Region | D1305 (#67) / Whole Map (#1190) |
| EventSpawn | false |
| Delay | 5 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5330 · Green Goddess Of Black Palace (#131) / D1305 (#67) / Whole Map (#1190)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Black Palace (#131) |
| Region | D1305 (#67) / Whole Map (#1190) |
| EventSpawn | false |
| Delay | 5 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5331 · Mutant Captain (#132) / D1305 (#67) / Whole Map (#1190)

| 字段 | 值 |
|---|---|
| Monster | Mutant Captain (#132) |
| Region | D1305 (#67) / Whole Map (#1190) |
| EventSpawn | false |
| Delay | 5 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5332 · Jinchon Devil (#199) / D1206 (#58) / Boss Area (#1278)

| 字段 | 值 |
|---|---|
| Monster | Jinchon Devil (#199) |
| Region | D1206 (#58) / Boss Area (#1278) |
| EventSpawn | true |
| Delay | 1 |
| Count | 1 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5333 · Stone Griffin (#133) / D1206 (#58) / Boss Area (#1278)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D1206 (#58) / Boss Area (#1278) |
| EventSpawn | false |
| Delay | 5 |
| Count | 7 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5334 · Stone Griffin (#133) / D1206 (#58) / Whole Map (#1275)

| 字段 | 值 |
|---|---|
| Monster | Stone Griffin (#133) |
| Region | D1206 (#58) / Whole Map (#1275) |
| EventSpawn | false |
| Delay | 5 |
| Count | 10 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5335 · Flame Griffin (#134) / D1206 (#58) / Whole Map (#1275)

| 字段 | 值 |
|---|---|
| Monster | Flame Griffin (#134) |
| Region | D1206 (#58) / Whole Map (#1275) |
| EventSpawn | false |
| Delay | 5 |
| Count | 10 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5336 · Pink Goddess Of Underground (#136) / D1206 (#58) / Whole Map (#1275)

| 字段 | 值 |
|---|---|
| Monster | Pink Goddess Of Underground (#136) |
| Region | D1206 (#58) / Whole Map (#1275) |
| EventSpawn | false |
| Delay | 5 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5337 · Green Goddess Of Underground (#138) / D1206 (#58) / Whole Map (#1275)

| 字段 | 值 |
|---|---|
| Monster | Green Goddess Of Underground (#138) |
| Region | D1206 (#58) / Whole Map (#1275) |
| EventSpawn | false |
| Delay | 5 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5338 · Vicious Mutant Captain (#137) / D1206 (#58) / Whole Map (#1275)

| 字段 | 值 |
|---|---|
| Monster | Vicious Mutant Captain (#137) |
| Region | D1206 (#58) / Whole Map (#1275) |
| EventSpawn | false |
| Delay | 5 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5340 · Evil Monkey (#84) / 11 (#291) / Open Area (#1471)

| 字段 | 值 |
|---|---|
| Monster | Evil Monkey (#84) |
| Region | 11 (#291) / Open Area (#1471) |
| EventSpawn | false |
| Delay | 15 |
| Count | 400 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5341 · Monkey (#83) / 11 (#291) / Open Area (#1471)

| 字段 | 值 |
|---|---|
| Monster | Monkey (#83) |
| Region | 11 (#291) / Open Area (#1471) |
| EventSpawn | false |
| Delay | 15 |
| Count | 400 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5342 · Evil Elephant (#85) / 11 (#291) / Ridges (#1473)

| 字段 | 值 |
|---|---|
| Monster | Evil Elephant (#85) |
| Region | 11 (#291) / Ridges (#1473) |
| EventSpawn | false |
| Delay | 15 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5343 · Evil Fanatic (#82) / 11 (#291) / Ridges (#1473)

| 字段 | 值 |
|---|---|
| Monster | Evil Fanatic (#82) |
| Region | 11 (#291) / Ridges (#1473) |
| EventSpawn | false |
| Delay | 15 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5344 · Cannibal Fanatic (#86) / 11 (#291) / Ridges (#1473)

| 字段 | 值 |
|---|---|
| Monster | Cannibal Fanatic (#86) |
| Region | 11 (#291) / Ridges (#1473) |
| EventSpawn | false |
| Delay | 15 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5345 · Crazed Warrior (#87) / 11 (#291) / Ridges (#1473)

| 字段 | 值 |
|---|---|
| Monster | Crazed Warrior (#87) |
| Region | 11 (#291) / Ridges (#1473) |
| EventSpawn | false |
| Delay | 30 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5346 · Brass Feral Warrior (#201) / D2401 (#294) / Whole Map (#1475)

| 字段 | 值 |
|---|---|
| Monster | Brass Feral Warrior (#201) |
| Region | D2401 (#294) / Whole Map (#1475) |
| EventSpawn | false |
| Delay | 1 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5347 · Obsidian Feral Warrior (#202) / D2401 (#294) / Whole Map (#1475)

| 字段 | 值 |
|---|---|
| Monster | Obsidian Feral Warrior (#202) |
| Region | D2401 (#294) / Whole Map (#1475) |
| EventSpawn | false |
| Delay | 1 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5348 · Sun Feral Warrior (#203) / D2401 (#294) / Whole Map (#1475)

| 字段 | 值 |
|---|---|
| Monster | Sun Feral Warrior (#203) |
| Region | D2401 (#294) / Whole Map (#1475) |
| EventSpawn | false |
| Delay | 1 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5349 · Moon Feral Warrior (#204) / D2401 (#294) / Whole Map (#1475)

| 字段 | 值 |
|---|---|
| Monster | Moon Feral Warrior (#204) |
| Region | D2401 (#294) / Whole Map (#1475) |
| EventSpawn | false |
| Delay | 1 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5351 · Flame Demon (#206) / D2401 (#294) / Whole Map (#1475)

| 字段 | 值 |
|---|---|
| Monster | Flame Demon (#206) |
| Region | D2401 (#294) / Whole Map (#1475) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5352 · Ferocious Flame Demon (#209) / D2402 (#295) / Whole Map (#1480)

| 字段 | 值 |
|---|---|
| Monster | Ferocious Flame Demon (#209) |
| Region | D2402 (#295) / Whole Map (#1480) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5353 · Brass Feral Warrior (#201) / D2402 (#295) / Whole Map (#1480)

| 字段 | 值 |
|---|---|
| Monster | Brass Feral Warrior (#201) |
| Region | D2402 (#295) / Whole Map (#1480) |
| EventSpawn | false |
| Delay | 1 |
| Count | 125 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5354 · Obsidian Feral Warrior (#202) / D2402 (#295) / Whole Map (#1480)

| 字段 | 值 |
|---|---|
| Monster | Obsidian Feral Warrior (#202) |
| Region | D2402 (#295) / Whole Map (#1480) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5355 · Sun Feral Warrior (#203) / D2402 (#295) / Whole Map (#1480)

| 字段 | 值 |
|---|---|
| Monster | Sun Feral Warrior (#203) |
| Region | D2402 (#295) / Whole Map (#1480) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5356 · Moon Feral Warrior (#204) / D2402 (#295) / Whole Map (#1480)

| 字段 | 值 |
|---|---|
| Monster | Moon Feral Warrior (#204) |
| Region | D2402 (#295) / Whole Map (#1480) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5357 · Ox Feral General (#205) / D2402 (#295) / Whole Map (#1480)

| 字段 | 值 |
|---|---|
| Monster | Ox Feral General (#205) |
| Region | D2402 (#295) / Whole Map (#1480) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5358 · Flame Demon (#206) / D2402 (#295) / Whole Map (#1480)

| 字段 | 值 |
|---|---|
| Monster | Flame Demon (#206) |
| Region | D2402 (#295) / Whole Map (#1480) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5359 · Ferocious Flame Demon (#209) / D2402 (#295) / Whole Map (#1480)

| 字段 | 值 |
|---|---|
| Monster | Ferocious Flame Demon (#209) |
| Region | D2402 (#295) / Whole Map (#1480) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5360 · Brass Feral Warrior (#201) / D2403 (#296) / Whole Map (#1485)

| 字段 | 值 |
|---|---|
| Monster | Brass Feral Warrior (#201) |
| Region | D2403 (#296) / Whole Map (#1485) |
| EventSpawn | false |
| Delay | 1 |
| Count | 155 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5361 · Obsidian Feral Warrior (#202) / D2403 (#296) / Whole Map (#1485)

| 字段 | 值 |
|---|---|
| Monster | Obsidian Feral Warrior (#202) |
| Region | D2403 (#296) / Whole Map (#1485) |
| EventSpawn | false |
| Delay | 1 |
| Count | 155 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5362 · Sun Feral Warrior (#203) / D2403 (#296) / Whole Map (#1485)

| 字段 | 值 |
|---|---|
| Monster | Sun Feral Warrior (#203) |
| Region | D2403 (#296) / Whole Map (#1485) |
| EventSpawn | false |
| Delay | 1 |
| Count | 155 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5363 · Moon Feral Warrior (#204) / D2403 (#296) / Whole Map (#1485)

| 字段 | 值 |
|---|---|
| Monster | Moon Feral Warrior (#204) |
| Region | D2403 (#296) / Whole Map (#1485) |
| EventSpawn | false |
| Delay | 1 |
| Count | 155 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5364 · Ox Feral General (#205) / D2403 (#296) / Whole Map (#1485)

| 字段 | 值 |
|---|---|
| Monster | Ox Feral General (#205) |
| Region | D2403 (#296) / Whole Map (#1485) |
| EventSpawn | false |
| Delay | 1 |
| Count | 288 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5365 · Flame Demon (#206) / D2403 (#296) / Whole Map (#1485)

| 字段 | 值 |
|---|---|
| Monster | Flame Demon (#206) |
| Region | D2403 (#296) / Whole Map (#1485) |
| EventSpawn | false |
| Delay | 1 |
| Count | 266 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5366 · Ferocious Flame Demon (#209) / D2403 (#296) / Whole Map (#1485)

| 字段 | 值 |
|---|---|
| Monster | Ferocious Flame Demon (#209) |
| Region | D2403 (#296) / Whole Map (#1485) |
| EventSpawn | false |
| Delay | 1 |
| Count | 277 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5367 · Winged Horror (#207) / D2403 (#296) / Whole Map (#1485)

| 字段 | 值 |
|---|---|
| Monster | Winged Horror (#207) |
| Region | D2403 (#296) / Whole Map (#1485) |
| EventSpawn | false |
| Delay | 160 |
| Count | 1 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5368 · Enraged Emperor Sa'Woo (#208) / D2403 (#296) / Whole Map (#1485)

| 字段 | 值 |
|---|---|
| Monster | Enraged Emperor Sa'Woo (#208) |
| Region | D2403 (#296) / Whole Map (#1485) |
| EventSpawn | false |
| Delay | 360 |
| Count | 1 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5369 · Icy Goddess (#168) / D005 (#242) / Mud Area (#830)

| 字段 | 值 |
|---|---|
| Monster | Icy Goddess (#168) |
| Region | D005 (#242) / Mud Area (#830) |
| EventSpawn | false |
| Delay | 1 |
| Count | 8 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5370 · Chicken (#8) / 10 (#259) / Town Area (#1495)

| 字段 | 值 |
|---|---|
| Monster | Chicken (#8) |
| Region | 10 (#259) / Town Area (#1495) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5371 · Cow (#11) / 10 (#259) / Town Area (#1495)

| 字段 | 值 |
|---|---|
| Monster | Cow (#11) |
| Region | 10 (#259) / Town Area (#1495) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5372 · Pig (#9) / 10 (#259) / Town Area (#1495)

| 字段 | 值 |
|---|---|
| Monster | Pig (#9) |
| Region | 10 (#259) / Town Area (#1495) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5373 · Tiger Snake (#19) / 10 (#259) / Low Lands (#1532)

| 字段 | 值 |
|---|---|
| Monster | Tiger Snake (#19) |
| Region | 10 (#259) / Low Lands (#1532) |
| EventSpawn | false |
| Delay | 1 |
| Count | 600 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5374 · Oma Hero (#23) / 10 (#259) / Low Lands (#1532)

| 字段 | 值 |
|---|---|
| Monster | Oma Hero (#23) |
| Region | 10 (#259) / Low Lands (#1532) |
| EventSpawn | false |
| Delay | 30 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5375 · Oma Warlord (#210) / 10 (#259) / Cliffs (#1533)

| 字段 | 值 |
|---|---|
| Monster | Oma Warlord (#210) |
| Region | 10 (#259) / Cliffs (#1533) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5376 · Oma Warlord (#210) / D2301 (#44) / Whole Map (#1512)

| 字段 | 值 |
|---|---|
| Monster | Oma Warlord (#210) |
| Region | D2301 (#44) / Whole Map (#1512) |
| EventSpawn | false |
| Delay | 1 |
| Count | 64 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5377 · Goru Spearman (#211) / D2301 (#44) / Whole Map (#1512)

| 字段 | 值 |
|---|---|
| Monster | Goru Spearman (#211) |
| Region | D2301 (#44) / Whole Map (#1512) |
| EventSpawn | false |
| Delay | 1 |
| Count | 64 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5378 · Goru Archer (#212) / D2301 (#44) / Whole Map (#1512)

| 字段 | 值 |
|---|---|
| Monster | Goru Archer (#212) |
| Region | D2301 (#44) / Whole Map (#1512) |
| EventSpawn | false |
| Delay | 1 |
| Count | 64 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5379 · Oma Warlord (#210) / D2302 (#260) / Whole Map (#1517)

| 字段 | 值 |
|---|---|
| Monster | Oma Warlord (#210) |
| Region | D2302 (#260) / Whole Map (#1517) |
| EventSpawn | false |
| Delay | 1 |
| Count | 144 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5380 · Goru Spearman (#211) / D2302 (#260) / Whole Map (#1517)

| 字段 | 值 |
|---|---|
| Monster | Goru Spearman (#211) |
| Region | D2302 (#260) / Whole Map (#1517) |
| EventSpawn | false |
| Delay | 1 |
| Count | 144 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5381 · Goru Archer (#212) / D2302 (#260) / Whole Map (#1517)

| 字段 | 值 |
|---|---|
| Monster | Goru Archer (#212) |
| Region | D2302 (#260) / Whole Map (#1517) |
| EventSpawn | false |
| Delay | 1 |
| Count | 144 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5382 · Goru General (#213) / D2302 (#260) / Whole Map (#1517)

| 字段 | 值 |
|---|---|
| Monster | Goru General (#213) |
| Region | D2302 (#260) / Whole Map (#1517) |
| EventSpawn | false |
| Delay | 1 |
| Count | 144 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5384 · Goru Spearman (#211) / D2303 (#261) / Whole Map (#1522)

| 字段 | 值 |
|---|---|
| Monster | Goru Spearman (#211) |
| Region | D2303 (#261) / Whole Map (#1522) |
| EventSpawn | false |
| Delay | 1 |
| Count | 355 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5385 · Goru Archer (#212) / D2303 (#261) / Whole Map (#1522)

| 字段 | 值 |
|---|---|
| Monster | Goru Archer (#212) |
| Region | D2303 (#261) / Whole Map (#1522) |
| EventSpawn | false |
| Delay | 1 |
| Count | 355 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5386 · Goru General (#213) / D2303 (#261) / Whole Map (#1522)

| 字段 | 值 |
|---|---|
| Monster | Goru General (#213) |
| Region | D2303 (#261) / Whole Map (#1522) |
| EventSpawn | false |
| Delay | 1 |
| Count | 355 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5387 · Goru Spearman (#211) / D2304 (#262) / Whole Map (#1527)

| 字段 | 值 |
|---|---|
| Monster | Goru Spearman (#211) |
| Region | D2304 (#262) / Whole Map (#1527) |
| EventSpawn | false |
| Delay | 1 |
| Count | 355 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5388 · Goru Archer (#212) / D2304 (#262) / Whole Map (#1527)

| 字段 | 值 |
|---|---|
| Monster | Goru Archer (#212) |
| Region | D2304 (#262) / Whole Map (#1527) |
| EventSpawn | false |
| Delay | 1 |
| Count | 355 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5389 · Goru General (#213) / D2304 (#262) / Whole Map (#1527)

| 字段 | 值 |
|---|---|
| Monster | Goru General (#213) |
| Region | D2304 (#262) / Whole Map (#1527) |
| EventSpawn | false |
| Delay | 1 |
| Count | 355 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5390 · Enraged Arch Lich Taedu (#215) / D2304 (#262) / Whole Map (#1527)

| 字段 | 值 |
|---|---|
| Monster | Enraged Arch Lich Taedu (#215) |
| Region | D2304 (#262) / Whole Map (#1527) |
| EventSpawn | false |
| Delay | 120 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5391 · Apparition Archer (#141) / D1802 (#121) / Whole Map (#1539)

| 字段 | 值 |
|---|---|
| Monster | Apparition Archer (#141) |
| Region | D1802 (#121) / Whole Map (#1539) |
| EventSpawn | false |
| Delay | 1 |
| Count | 45 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5392 · Apparition Bladesman (#142) / D1802 (#121) / Whole Map (#1539)

| 字段 | 值 |
|---|---|
| Monster | Apparition Bladesman (#142) |
| Region | D1802 (#121) / Whole Map (#1539) |
| EventSpawn | false |
| Delay | 1 |
| Count | 45 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5393 · Apparition Soldier (#143) / D1802 (#121) / Whole Map (#1539)

| 字段 | 值 |
|---|---|
| Monster | Apparition Soldier (#143) |
| Region | D1802 (#121) / Whole Map (#1539) |
| EventSpawn | false |
| Delay | 1 |
| Count | 76 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5394 · Escort Commander (#216) / D2201 (#219) / Whole Map (#1564)

| 字段 | 值 |
|---|---|
| Monster | Escort Commander (#216) |
| Region | D2201 (#219) / Whole Map (#1564) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5395 · Fiery Dancer (#217) / D2201 (#219) / Whole Map (#1564)

| 字段 | 值 |
|---|---|
| Monster | Fiery Dancer (#217) |
| Region | D2201 (#219) / Whole Map (#1564) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5396 · Emerald Dancer (#218) / D2201 (#219) / Whole Map (#1564)

| 字段 | 值 |
|---|---|
| Monster | Emerald Dancer (#218) |
| Region | D2201 (#219) / Whole Map (#1564) |
| EventSpawn | false |
| Delay | 1 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5397 · Escort Commander (#216) / D22021 (#273) / Whole Map (#1565)

| 字段 | 值 |
|---|---|
| Monster | Escort Commander (#216) |
| Region | D22021 (#273) / Whole Map (#1565) |
| EventSpawn | false |
| Delay | 1 |
| Count | 222 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5398 · Fiery Dancer (#217) / D22021 (#273) / Whole Map (#1565)

| 字段 | 值 |
|---|---|
| Monster | Fiery Dancer (#217) |
| Region | D22021 (#273) / Whole Map (#1565) |
| EventSpawn | false |
| Delay | 1 |
| Count | 222 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5399 · Emerald Dancer (#218) / D22021 (#273) / Whole Map (#1565)

| 字段 | 值 |
|---|---|
| Monster | Emerald Dancer (#218) |
| Region | D22021 (#273) / Whole Map (#1565) |
| EventSpawn | false |
| Delay | 1 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5400 · Escort Commander (#216) / D2204 (#277) / Whole Map (#1566)

| 字段 | 值 |
|---|---|
| Monster | Escort Commander (#216) |
| Region | D2204 (#277) / Whole Map (#1566) |
| EventSpawn | false |
| Delay | 1 |
| Count | 444 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5401 · Fiery Dancer (#217) / D2204 (#277) / Whole Map (#1566)

| 字段 | 值 |
|---|---|
| Monster | Fiery Dancer (#217) |
| Region | D2204 (#277) / Whole Map (#1566) |
| EventSpawn | false |
| Delay | 1 |
| Count | 555 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5402 · Emerald Dancer (#218) / D2204 (#277) / Whole Map (#1566)

| 字段 | 值 |
|---|---|
| Monster | Emerald Dancer (#218) |
| Region | D2204 (#277) / Whole Map (#1566) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5403 · Escort Commander (#216) / D2205 (#278) / Whole Map (#1567)

| 字段 | 值 |
|---|---|
| Monster | Escort Commander (#216) |
| Region | D2205 (#278) / Whole Map (#1567) |
| EventSpawn | false |
| Delay | 5 |
| Count | 23 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5404 · Fiery Dancer (#217) / D2205 (#278) / Whole Map (#1567)

| 字段 | 值 |
|---|---|
| Monster | Fiery Dancer (#217) |
| Region | D2205 (#278) / Whole Map (#1567) |
| EventSpawn | false |
| Delay | 3 |
| Count | 24 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5405 · Emerald Dancer (#218) / D2205 (#278) / Whole Map (#1567)

| 字段 | 值 |
|---|---|
| Monster | Emerald Dancer (#218) |
| Region | D2205 (#278) / Whole Map (#1567) |
| EventSpawn | false |
| Delay | 4 |
| Count | 25 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5406 · Queen Of Dawn (#219) / D2205 (#278) / Boss Area (#1568)

| 字段 | 值 |
|---|---|
| Monster | Queen Of Dawn (#219) |
| Region | D2205 (#278) / Boss Area (#1568) |
| EventSpawn | false |
| Delay | 166 |
| Count | 1 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #5407 · Jinhwan Spirit (#225) / D006 (#332) / Whole Map (#1569)

| 字段 | 值 |
|---|---|
| Monster | Jinhwan Spirit (#225) |
| Region | D006 (#332) / Whole Map (#1569) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5408 · Jinhwan Guardian (#226) / D006 (#332) / Whole Map (#1569)

| 字段 | 值 |
|---|---|
| Monster | Jinhwan Guardian (#226) |
| Region | D006 (#332) / Whole Map (#1569) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5409 · Oyoung Beast (#221) / D006 (#332) / Whole Map (#1569)

| 字段 | 值 |
|---|---|
| Monster | Oyoung Beast (#221) |
| Region | D006 (#332) / Whole Map (#1569) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5410 · Oyoung General (#227) / D006 (#332) / Whole Map (#1569)

| 字段 | 值 |
|---|---|
| Monster | Oyoung General (#227) |
| Region | D006 (#332) / Whole Map (#1569) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5411 · Jinhwan Spirit (#225) / D007 (#333) / Whole Map (#1573)

| 字段 | 值 |
|---|---|
| Monster | Jinhwan Spirit (#225) |
| Region | D007 (#333) / Whole Map (#1573) |
| EventSpawn | false |
| Delay | 1 |
| Count | 130 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5412 · Jinhwan Guardian (#226) / D007 (#333) / Whole Map (#1573)

| 字段 | 值 |
|---|---|
| Monster | Jinhwan Guardian (#226) |
| Region | D007 (#333) / Whole Map (#1573) |
| EventSpawn | false |
| Delay | 1 |
| Count | 130 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5413 · Oyoung Beast (#221) / D007 (#333) / Whole Map (#1573)

| 字段 | 值 |
|---|---|
| Monster | Oyoung Beast (#221) |
| Region | D007 (#333) / Whole Map (#1573) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5414 · Oyoung General (#227) / D007 (#333) / Whole Map (#1573)

| 字段 | 值 |
|---|---|
| Monster | Oyoung General (#227) |
| Region | D007 (#333) / Whole Map (#1573) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5415 · Jinhwan Spirit (#225) / D2900 (#334) / Whole Map (#1578)

| 字段 | 值 |
|---|---|
| Monster | Jinhwan Spirit (#225) |
| Region | D2900 (#334) / Whole Map (#1578) |
| EventSpawn | false |
| Delay | 1 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5416 · Jinhwan Guardian (#226) / D2900 (#334) / Whole Map (#1578)

| 字段 | 值 |
|---|---|
| Monster | Jinhwan Guardian (#226) |
| Region | D2900 (#334) / Whole Map (#1578) |
| EventSpawn | false |
| Delay | 1 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #5417 · Oyoung Beast (#221) / D2900 (#334) / Whole Map (#1578)

| 字段 | 值 |
|---|---|
| Monster | Oyoung Beast (#221) |
| Region | D2900 (#334) / Whole Map (#1578) |
| EventSpawn | false |
| Delay | 1 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

