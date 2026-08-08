<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改。重新生成: dotnet run --project Tools/SystemDbProbe -- --dump docs/database -->

# 刷新点（RespawnInfo）

> 记录 #4167 – #4493，共 1471 条（第 1/5 部分）。

[README](../README.md) · [下一部分 →](RespawnInfo.2.md)

## 快速浏览

| # | Monster | Region | Delay | Count | DropSet | EventSpawn |
|---|---|---|---|---|---|---|
| 4167 | Chicken (#8) | 0 (#1) / Spawn Ring 1 (#31) | 1 | 250 | 0 | false |
| 4168 | Pig (#9) | 0 (#1) / Spawn Ring 1 (#31) | 1 | 150 | 0 | false |
| 4169 | Cow (#11) | 0 (#1) / Spawn Ring 1 (#31) | 1 | 40 | 0 | false |
| 4170 | Deer (#10) | 0 (#1) / Spawn Ring 1 (#31) | 1 | 20 | 0 | false |
| 4171 | Scarecrow (#21) | 0 (#1) / Spawn Ring 2 (#32) | 1 | 400 | 0 | false |
| 4172 | Claw Cat (#13) | 0 (#1) / Spawn Ring 2 (#32) | 1 | 250 | 0 | false |
| 4174 | Deer (#10) | 0 (#1) / Spawn Ring 2 (#32) | 1 | 150 | 0 | false |
| 4175 | Oma (#22) | 0 (#1) / Spawn Ring 2 (#32) | 1 | 150 | 0 | false |
| 4176 | Forest Yeti (#15) | 0 (#1) / Spawn Ring 2 (#32) | 1 | 60 | 0 | false |
| 4179 | Tiger Snake (#19) | 0 (#1) / Spawn Ring 3 (#33) | 1 | 200 | 0 | false |
| 4180 | Oma Warrior (#18) | 0 (#1) / Spawn Ring 3 (#33) | 1 | 200 | 0 | false |
| 4181 | Spitting Spider (#20) | 0 (#1) / Spawn Ring 3 (#33) | 1 | 200 | 0 | false |
| 4182 | Wolf (#14) | 0 (#1) / Spawn Ring 3 (#33) | 1 | 200 | 0 | false |
| 4183 | Oma Hero (#23) | 0 (#1) / Spawn Ring 3 (#33) | 30 | 2 | 0 | false |
| 4184 | Chestnut Tree (#16) | 0 (#1) / Grass Area (#79) | 30 | 150 | 0 | false |
| 4185 | Carnivorous Plant (#17) | 0 (#1) / Grass Area (#79) | 1 | 600 | 0 | false |
| 4186 | Centipede (#51) | D801 (#160) / Whole Map (#427) | 1 | 150 | 0 | false |
| 4187 | Butterfly Worm (#52) | D801 (#160) / Whole Map (#427) | 1 | 150 | 0 | false |
| 4188 | Wasp Hatchling (#50) | D801 (#160) / Whole Map (#427) | 1 | 150 | 0 | false |
| 4189 | Mutant Maggot (#53) | D801 (#160) / Whole Map (#427) | 1 | 150 | 0 | false |
| 4190 | Centipede (#51) | D801 (#160) / Respawn Area 1 (#434) | 15 | 20 | 0 | false |
| 4191 | Butterfly Worm (#52) | D801 (#160) / Respawn Area 1 (#434) | 15 | 20 | 0 | false |
| 4192 | Wasp Hatchling (#50) | D801 (#160) / Respawn Area 1 (#434) | 15 | 20 | 0 | false |
| 4193 | Mutant Maggot (#53) | D801 (#160) / Respawn Area 1 (#434) | 15 | 20 | 0 | false |
| 4194 | Centipede (#51) | D802 (#161) / Whole Map (#435) | 1 | 200 | 0 | false |
| 4195 | Butterfly Worm (#52) | D802 (#161) / Whole Map (#435) | 1 | 200 | 0 | false |
| 4196 | Wasp Hatchling (#50) | D802 (#161) / Whole Map (#435) | 1 | 200 | 0 | false |
| 4197 | Mutant Maggot (#53) | D802 (#161) / Whole Map (#435) | 1 | 200 | 0 | false |
| 4198 | Centipede (#51) | D803 (#162) / Whole Map (#444) | 1 | 225 | 0 | false |
| 4199 | Butterfly Worm (#52) | D803 (#162) / Whole Map (#444) | 1 | 225 | 0 | false |
| 4200 | Wasp Hatchling (#50) | D803 (#162) / Whole Map (#444) | 1 | 225 | 0 | false |
| 4201 | Mutant Maggot (#53) | D803 (#162) / Whole Map (#444) | 1 | 225 | 0 | false |
| 4202 | Earwig (#54) | D803 (#162) / Whole Map (#444) | 1 | 100 | 0 | false |
| 4203 | Centipede (#51) | D803 (#162) / Respawn Area 1 (#451) | 15 | 15 | 0 | false |
| 4204 | Butterfly Worm (#52) | D803 (#162) / Respawn Area 1 (#451) | 15 | 15 | 0 | false |
| 4205 | Wasp Hatchling (#50) | D803 (#162) / Respawn Area 1 (#451) | 15 | 15 | 0 | false |
| 4206 | Mutant Maggot (#53) | D803 (#162) / Respawn Area 1 (#451) | 15 | 15 | 0 | false |
| 4207 | Earwig (#54) | D803 (#162) / Respawn Area 1 (#451) | 15 | 5 | 0 | false |
| 4208 | Centipede (#51) | D804 (#163) / Whole Map (#452) | 1 | 225 | 0 | false |
| 4209 | Butterfly Worm (#52) | D804 (#163) / Whole Map (#452) | 1 | 225 | 0 | false |
| 4210 | Wasp Hatchling (#50) | D804 (#163) / Whole Map (#452) | 1 | 225 | 0 | false |
| 4211 | Mutant Maggot (#53) | D804 (#163) / Whole Map (#452) | 1 | 225 | 0 | false |
| 4212 | Earwig (#54) | D804 (#163) / Whole Map (#452) | 1 | 100 | 0 | false |
| 4213 | Centipede (#51) | D804 (#163) / Respawn Area 1 (#457) | 15 | 15 | 0 | false |
| 4214 | Butterfly Worm (#52) | D804 (#163) / Respawn Area 1 (#457) | 15 | 15 | 0 | false |
| 4215 | Wasp Hatchling (#50) | D804 (#163) / Respawn Area 1 (#457) | 15 | 15 | 0 | false |
| 4216 | Mutant Maggot (#53) | D804 (#163) / Respawn Area 1 (#457) | 15 | 15 | 0 | false |
| 4217 | Earwig (#54) | D804 (#163) / Respawn Area 1 (#457) | 15 | 5 | 0 | false |
| 4218 | Centipede (#51) | D804 (#163) / Respawn Area 2 (#458) | 15 | 15 | 0 | false |
| 4219 | Butterfly Worm (#52) | D804 (#163) / Respawn Area 2 (#458) | 15 | 15 | 0 | false |
| 4220 | Wasp Hatchling (#50) | D804 (#163) / Respawn Area 2 (#458) | 15 | 15 | 0 | false |
| 4221 | Mutant Maggot (#53) | D804 (#163) / Respawn Area 2 (#458) | 15 | 15 | 0 | false |
| 4222 | Earwig (#54) | D804 (#163) / Respawn Area 2 (#458) | 15 | 5 | 0 | false |
| 4223 | Centipede (#51) | D805 (#164) / Whole Map (#90) | 1 | 225 | 0 | false |
| 4224 | Butterfly Worm (#52) | D805 (#164) / Whole Map (#90) | 1 | 225 | 0 | false |
| 4225 | Wasp Hatchling (#50) | D805 (#164) / Whole Map (#90) | 1 | 225 | 0 | false |
| 4226 | Mutant Maggot (#53) | D805 (#164) / Whole Map (#90) | 1 | 225 | 0 | false |
| 4227 | Earwig (#54) | D805 (#164) / Whole Map (#90) | 1 | 100 | 0 | false |
| 4228 | Centipede (#51) | D805 (#164) / Whole Map (#90) | 15 | 15 | 0 | false |
| 4229 | Butterfly Worm (#52) | D805 (#164) / Respawn Area  (#463) | 15 | 15 | 0 | false |
| 4230 | Wasp Hatchling (#50) | D805 (#164) / Respawn Area  (#463) | 15 | 15 | 0 | false |
| 4231 | Mutant Maggot (#53) | D805 (#164) / Respawn Area  (#463) | 15 | 15 | 0 | false |
| 4232 | Earwig (#54) | D805 (#164) / Respawn Area  (#463) | 15 | 5 | 0 | false |
| 4233 | Centipede (#51) | D805 (#164) / Lord Ji'Nae Area (#462) | 5 | 15 | 0 | false |
| 4234 | Earwig (#54) | D805 (#164) / Lord Ji'Nae Area (#462) | 5 | 15 | 0 | false |
| 4235 | Lord Ji'Nae (#56) | D805 (#164) / Lord Ji'Nae (#461) | 300 | 1 | 0 | false |
| 4236 | Iron Lance (#55) | D805 (#164) / Whole Map (#90) | 30 | 2 | 0 | false |
| 4237 | Ant Soldier (#38) | D401 (#142) / Whole Map (#464) | 1 | 160 | 0 | false |
| 4238 | Ant Needler (#40) | D401 (#142) / Whole Map (#464) | 1 | 40 | 0 | false |
| 4239 | Armoured Ant (#41) | D402 (#143) / Whole Map (#469) | 1 | 50 | 0 | false |
| 4240 | Ant Soldier (#38) | D402 (#143) / Whole Map (#469) | 1 | 240 | 0 | false |
| 4241 | Ant Needler (#40) | D402 (#143) / Whole Map (#469) | 1 | 60 | 0 | false |
| 4242 | Armoured Ant (#41) | D403 (#144) / Whole Map (#478) | 1 | 200 | 0 | false |
| 4243 | Ant Soldier (#38) | D403 (#144) / Whole Map (#478) | 1 | 180 | 0 | false |
| 4244 | Ant Needler (#40) | D403 (#144) / Whole Map (#478) | 1 | 80 | 0 | false |
| 4245 | Ant Healer (#39) | D403 (#144) / Whole Map (#478) | 1 | 20 | 0 | false |
| 4246 | Armoured Ant (#41) | D404 (#145) / Whole Map (#490) | 1 | 250 | 0 | false |
| 4247 | Ant Soldier (#38) | D404 (#145) / Whole Map (#490) | 1 | 225 | 0 | false |
| 4248 | Ant Needler (#40) | D404 (#145) / Whole Map (#490) | 1 | 120 | 0 | false |
| 4249 | Ant Healer (#39) | D404 (#145) / Whole Map (#490) | 1 | 40 | 0 | false |
| 4250 | Ant Commander (#42) | D404 (#145) / Whole Map (#490) | 30 | 2 | 0 | false |
| 4252 | Skeleton (#27) | D101 (#26) / Whole Map (#99) | 1 | 120 | 0 | false |
| 4253 | Cave Bat (#24) | D101 (#26) / Whole Map (#99) | 1 | 40 | 0 | false |
| 4254 | Scorpion (#25) | D101 (#26) / Whole Map (#99) | 1 | 40 | 0 | false |
| 4256 | Skeleton (#27) | D102 (#31) / Whole Map (#369) | 1 | 150 | 0 | false |
| 4257 | Cave Bat (#24) | D102 (#31) / Whole Map (#369) | 1 | 20 | 0 | false |
| 4258 | Scorpion (#25) | D102 (#31) / Whole Map (#369) | 1 | 20 | 0 | false |
| 4259 | Skeleton Axe Thrower (#28) | D102 (#31) / Whole Map (#369) | 1 | 65 | 0 | false |
| 4261 | Skeleton (#27) | D103 (#32) / Whole Map (#378) | 1 | 180 | 0 | false |
| 4262 | Cave Bat (#24) | D103 (#32) / Whole Map (#378) | 1 | 20 | 0 | false |
| 4263 | Skeleton Warrior (#29) | D103 (#32) / Whole Map (#378) | 1 | 180 | 0 | false |
| 4264 | Skeleton Axe Thrower (#28) | D103 (#32) / Whole Map (#378) | 1 | 100 | 0 | false |
| 4265 | Skeleton Lord (#30) | D103 (#32) / Whole Map (#378) | 30 | 2 | 0 | false |
| 4266 | Cave Maggot (#31) | D201 (#136) / Whole Map (#498) | 1 | 40 | 0 | false |
| 4267 | GhostSorcerer (#32) | D201 (#136) / Whole Map (#498) | 1 | 40 | 2 | false |
| 4268 | Ghost Mage (#33) | D201 (#136) / Whole Map (#498) | 1 | 60 | 2 | false |
| 4269 | Devouring Ghost (#35) | D201 (#136) / Whole Map (#498) | 1 | 150 | 2 | false |
| 4270 | Corpse Raising Ghost (#36) | D201 (#136) / Whole Map (#498) | 1 | 150 | 2 | false |
| 4271 | Voracious Ghost (#34) | D201 (#136) / Whole Map (#498) | 1 | 150 | 2 | false |
| 4272 | Cave Maggot (#31) | D202 (#137) / Whole Map (#507) | 1 | 60 | 0 | false |
| 4273 | GhostSorcerer (#32) | D202 (#137) / Whole Map (#507) | 1 | 80 | 4 | false |
| 4274 | Ghost Mage (#33) | D202 (#137) / Whole Map (#507) | 1 | 100 | 4 | false |
| 4275 | Devouring Ghost (#35) | D202 (#137) / Whole Map (#507) | 1 | 175 | 4 | false |
| 4276 | Corpse Raising Ghost (#36) | D202 (#137) / Whole Map (#507) | 1 | 175 | 4 | false |
| 4277 | Voracious Ghost (#34) | D202 (#137) / Whole Map (#507) | 1 | 175 | 4 | false |
| 4278 | Cave Maggot (#31) | D203 (#138) / Whole Map (#515) | 1 | 60 | 0 | false |
| 4279 | GhostSorcerer (#32) | D203 (#138) / Whole Map (#515) | 1 | 150 | 0 | false |
| 4280 | Ghost Mage (#33) | D203 (#138) / Whole Map (#515) | 1 | 150 | 0 | false |
| 4281 | Devouring Ghost (#35) | D203 (#138) / Whole Map (#515) | 1 | 200 | 0 | false |
| 4282 | Corpse Raising Ghost (#36) | D203 (#138) / Whole Map (#515) | 1 | 200 | 0 | false |
| 4283 | Voracious Ghost (#34) | D203 (#138) / Whole Map (#515) | 1 | 200 | 0 | false |
| 4284 | Ghoul Champion (#37) | D203 (#138) / Whole Map (#515) | 30 | 2 | 0 | false |
| 4285 | Dark Arachnid (#72) | D001 (#12) / Whole Map (#229) | 1 | 225 | 0 | false |
| 4286 | Spider Bat (#66) | D001 (#12) / Whole Map (#229) | 1 | 100 | 0 | false |
| 4287 | Arachnid Gazer (#67) | D001 (#12) / Whole Map (#229) | 1 | 50 | 0 | false |
| 4288 | Venomous Arachnid (#71) | D001 (#12) / Whole Map (#229) | 1 | 70 | 0 | false |
| 4289 | Arachnid Broodmother (#73) | D001 (#12) / Respawn Areas (#230) | 15 | 2 | 0 | false |
| 4291 | Dark Arachnid (#72) | D001 (#12) / Respawn Areas (#230) | 15 | 150 | 0 | false |
| 4292 | Spider Bat (#66) | D001 (#12) / Respawn Areas (#230) | 15 | 60 | 0 | false |
| 4293 | Arachnid Gazer (#67) | D001 (#12) / Respawn Areas (#230) | 15 | 5 | 0 | false |
| 4294 | Venomous Arachnid (#71) | D001 (#12) / Respawn Areas (#230) | 15 | 40 | 0 | false |
| 4295 | Dark Arachnid (#72) | D901 (#165) / Whole Map (#596) | 1 | 200 | 1 | false |
| 4296 | Spider Bat (#66) | D901 (#165) / Whole Map (#596) | 1 | 100 | 1 | false |
| 4297 | Arachnid Gazer (#67) | D901 (#165) / Whole Map (#596) | 1 | 20 | 1 | false |
| 4298 | Venomous Arachnid (#71) | D901 (#165) / Whole Map (#596) | 1 | 120 | 1 | false |
| 4299 | Dark Arachnid (#72) | D902 (#166) / Whole Map (#605) | 1 | 125 | 1 | false |
| 4300 | Spider Bat (#66) | D902 (#166) / Whole Map (#605) | 1 | 60 | 1 | false |
| 4301 | Arachnid Gazer (#67) | D902 (#166) / Whole Map (#605) | 1 | 10 | 1 | false |
| 4302 | Venomous Arachnid (#71) | D902 (#166) / Whole Map (#605) | 1 | 100 | 1 | false |
| 4303 | Red Moon Guardian (#69) | D902 (#166) / Whole Map (#605) | 1 | 80 | 0 | false |
| 4304 | Dark Arachnid (#72) | D903 (#167) / Whole Map (#614) | 1 | 150 | 1 | false |
| 4305 | Spider Bat (#66) | D903 (#167) / Whole Map (#614) | 1 | 80 | 1 | false |
| 4306 | Arachnid Gazer (#67) | D903 (#167) / Whole Map (#614) | 1 | 20 | 1 | false |
| 4307 | Venomous Arachnid (#71) | D903 (#167) / Whole Map (#614) | 1 | 75 | 1 | false |
| 4308 | Red Moon Guardian (#69) | D903 (#167) / Whole Map (#614) | 1 | 120 | 0 | false |
| 4309 | Red Moon Protector (#70) | D903 (#167) / Whole Map (#614) | 1 | 120 | 0 | false |
| 4310 | Dark Arachnid (#72) | D904 (#168) / Whole Map (#621) | 1 | 255 | 1 | false |
| 4311 | Spider Bat (#66) | D904 (#168) / Whole Map (#621) | 1 | 255 | 1 | false |
| 4312 | Arachnid Gazer (#67) | D904 (#168) / Whole Map (#621) | 1 | 70 | 1 | false |
| 4313 | Venomous Arachnid (#71) | D904 (#168) / Whole Map (#621) | 1 | 200 | 1 | false |
| 4314 | Red Moon Guardian (#69) | D904 (#168) / Whole Map (#621) | 1 | 350 | 0 | false |
| 4315 | Red Moon Protector (#70) | D904 (#168) / Whole Map (#621) | 1 | 250 | 0 | false |
| 4316 | Red Moon Royal Guard (#74) | D904 (#168) / Whole Map (#621) | 30 | 2 | 0 | false |
| 4317 | Dark Arachnid (#72) | D905 (#559) / Whole Map (#628) | 5 | 7 | 1 | false |
| 4318 | Spider Bat (#66) | D905 (#559) / Whole Map (#628) | 5 | 7 | 1 | false |
| 4319 | Arachnid Gazer (#67) | D905 (#559) / Whole Map (#628) | 5 | 7 | 1 | false |
| 4320 | Venomous Arachnid (#71) | D905 (#559) / Whole Map (#628) | 5 | 7 | 1 | false |
| 4321 | Red Moon Guardian (#69) | D905 (#559) / Whole Map (#628) | 5 | 7 | 0 | false |
| 4322 | Red Moon Protector (#70) | D905 (#559) / Whole Map (#628) | 5 | 7 | 0 | false |
| 4323 | Red Moon The Fallen (#75) | D905 (#559) / Red Moon (#710) | 300 | 1 | 0 | true |
| 4324 | Vicious Rat (#79) | D1101 (#33) / Whole Map (#387) | 1 | 200 | 0 | false |
| 4325 | Zuma Sharpshooter (#76) | D1101 (#33) / Whole Map (#387) | 1 | 50 | 0 | false |
| 4326 | Vicious Rat (#79) | D1101 (#33) / Respawn Area 2 (#393) | 15 | 100 | 0 | false |
| 4327 | Zuma Sharpshooter (#76) | D1101 (#33) / Respawn Area 2 (#393) | 15 | 40 | 0 | false |
| 4328 | Vicious Rat (#79) | D1102 (#34) / Whole Map (#394) | 1 | 350 | 0 | false |
| 4329 | Zuma Sharpshooter (#76) | D1102 (#34) / Whole Map (#394) | 1 | 160 | 0 | false |
| 4330 | Vicious Rat (#79) | D1103 (#35) / Whole Map (#401) | 1 | 100 | 0 | false |
| 4331 | Zuma Sharpshooter (#76) | D1103 (#35) / Whole Map (#401) | 1 | 50 | 0 | false |
| 4332 | Zuma Fanatic (#77) | D1103 (#35) / Whole Map (#401) | 1 | 100 | 0 | false |
| 4333 | Zuma Guardian (#78) | D1103 (#35) / Whole Map (#401) | 1 | 100 | 0 | false |
| 4334 | Vicious Rat (#79) | D1103 (#35) / Respawn Area 1 (#406) | 15 | 20 | 0 | false |
| 4335 | Zuma Sharpshooter (#76) | D1103 (#35) / Respawn Area 1 (#406) | 15 | 10 | 0 | false |
| 4336 | Zuma Fanatic (#77) | D1103 (#35) / Respawn Area 1 (#406) | 15 | 20 | 0 | false |
| 4337 | Zuma Guardian (#78) | D1103 (#35) / Respawn Area 1 (#406) | 15 | 20 | 0 | false |
| 4338 | Vicious Rat (#79) | D1104 (#36) / Whole Map (#407) | 1 | 140 | 0 | false |
| 4339 | Zuma Sharpshooter (#76) | D1104 (#36) / Whole Map (#407) | 1 | 360 | 0 | false |
| 4340 | Zuma Fanatic (#77) | D1104 (#36) / Whole Map (#407) | 1 | 400 | 0 | false |
| 4341 | Zuma Guardian (#78) | D1104 (#36) / Whole Map (#407) | 1 | 400 | 0 | false |
| 4342 | Vicious Rat (#79) | D1104 (#36) / Respawn Area 1 (#412) | 15 | 30 | 0 | false |
| 4343 | Zuma Sharpshooter (#76) | D1104 (#36) / Respawn Area 1 (#412) | 15 | 30 | 0 | false |
| 4344 | Zuma Fanatic (#77) | D1104 (#36) / Respawn Area 1 (#412) | 15 | 40 | 0 | false |
| 4345 | Zuma Guardian (#78) | D1104 (#36) / Respawn Area 1 (#412) | 15 | 40 | 0 | false |
| 4347 | Zuma Sharpshooter (#76) | D1105 (#37) / Whole Map (#413) | 1 | 200 | 0 | false |
| 4348 | Zuma Fanatic (#77) | D1105 (#37) / Whole Map (#413) | 1 | 200 | 0 | false |
| 4349 | Zuma Guardian (#78) | D1105 (#37) / Whole Map (#413) | 1 | 200 | 0 | false |
| 4350 | Zuma Keeper (#80) | D1105 (#37) / Whole Map (#413) | 30 | 2 | 0 | false |
| 4352 | Zuma Sharpshooter (#76) | D1105 (#37) / Respawn Area 1 (#418) | 15 | 10 | 0 | false |
| 4353 | Zuma Fanatic (#77) | D1105 (#37) / Respawn Area 1 (#418) | 15 | 20 | 0 | false |
| 4354 | Zuma Guardian (#78) | D1105 (#37) / Respawn Area 1 (#418) | 15 | 20 | 0 | false |
| 4355 | Zuma Sharpshooter (#76) | D1105 (#37) / Respawn Area 2 (#419) | 15 | 15 | 0 | false |
| 4356 | Zuma Fanatic (#77) | D1105 (#37) / Respawn Area 2 (#419) | 15 | 30 | 0 | false |
| 4357 | Zuma Guardian (#78) | D1105 (#37) / Respawn Area 2 (#419) | 15 | 30 | 0 | false |
| 4358 | Zuma Sharpshooter (#76) | D1105 (#37) / Respawn Area 3 (#420) | 15 | 20 | 0 | false |
| 4359 | Zuma Fanatic (#77) | D1105 (#37) / Respawn Area 3 (#420) | 15 | 40 | 0 | false |
| 4360 | Zuma Guardian (#78) | D1105 (#37) / Respawn Area 3 (#420) | 15 | 40 | 0 | false |
| 4361 | Zuma Sharpshooter (#76) | D1105 (#37) / Respawn Area 4 (#421) | 15 | 25 | 0 | false |
| 4362 | Zuma Fanatic (#77) | D1105 (#37) / Respawn Area 4 (#421) | 15 | 50 | 0 | false |
| 4363 | Zuma Guardian (#78) | D1105 (#37) / Respawn Area 4 (#421) | 15 | 50 | 0 | false |
| 4364 | Zuma Sharpshooter (#76) | D1105 (#37) / Respawn Area 5 (#422) | 15 | 30 | 0 | false |
| 4365 | Zuma Fanatic (#77) | D1105 (#37) / Respawn Area 5 (#422) | 15 | 60 | 0 | false |
| 4366 | Zuma Guardian (#78) | D1105 (#37) / Respawn Area 5 (#422) | 15 | 60 | 0 | false |
| 4367 | Zuma King (#81) | D1106 (#38) / Zumataurus (#426) | 300 | 1 | 0 | true |
| 4368 | Zuma Sharpshooter (#76) | D1106 (#38) / Whole Map (#423) | 5 | 10 | 0 | false |
| 4369 | Zuma Fanatic (#77) | D1106 (#38) / Whole Map (#423) | 5 | 7 | 0 | false |
| 4370 | Zuma Guardian (#78) | D1106 (#38) / Whole Map (#423) | 5 | 7 | 0 | false |
| 4371 | Minotaur (#107) | D1001 (#16) / Whole Map (#258) | 1 | 250 | 0 | false |
| 4372 | Frost Minotaur (#108) | D1001 (#16) / Whole Map (#258) | 1 | 66 | 0 | false |
| 4373 | Shock Minotaur (#110) | D1001 (#16) / Whole Map (#258) | 1 | 66 | 0 | false |
| 4374 | Fury Minotaur (#112) | D1001 (#16) / Whole Map (#258) | 1 | 66 | 0 | false |
| 4375 | Flame Minotaur (#113) | D1001 (#16) / Whole Map (#258) | 1 | 66 | 0 | false |
| 4377 | Minotaur (#107) | D1001 (#16) / Respawn Area 1 (#259) | 10 | 4 | 0 | false |
| 4378 | Frost Minotaur (#108) | D1001 (#16) / Respawn Area 1 (#259) | 10 | 7 | 0 | false |
| 4379 | Shock Minotaur (#110) | D1001 (#16) / Respawn Area 1 (#259) | 10 | 7 | 0 | false |
| 4380 | Fury Minotaur (#112) | D1001 (#16) / Respawn Area 1 (#259) | 10 | 7 | 0 | false |
| 4381 | Flame Minotaur (#113) | D1001 (#16) / Respawn Area 1 (#259) | 10 | 7 | 0 | false |
| 4382 | Minotaur (#107) | D1001 (#16) / Respawn Area 2 (#260) | 10 | 4 | 0 | false |
| 4383 | Frost Minotaur (#108) | D1001 (#16) / Respawn Area 2 (#260) | 10 | 7 | 0 | false |
| 4384 | Shock Minotaur (#110) | D1001 (#16) / Respawn Area 2 (#260) | 10 | 7 | 0 | false |
| 4385 | Fury Minotaur (#112) | D1001 (#16) / Respawn Area 2 (#260) | 10 | 7 | 0 | false |
| 4386 | Flame Minotaur (#113) | D1001 (#16) / Respawn Area 2 (#260) | 10 | 7 | 0 | false |
| 4387 | Minotaur (#107) | D1001 (#16) / Respawn Area 3 (#261) | 10 | 4 | 0 | false |
| 4388 | Frost Minotaur (#108) | D1001 (#16) / Respawn Area 3 (#261) | 10 | 7 | 0 | false |
| 4389 | Shock Minotaur (#110) | D1001 (#16) / Respawn Area 3 (#261) | 10 | 7 | 0 | false |
| 4390 | Fury Minotaur (#112) | D1001 (#16) / Respawn Area 3 (#261) | 10 | 7 | 0 | false |
| 4391 | Flame Minotaur (#113) | D1001 (#16) / Respawn Area 3 (#261) | 10 | 7 | 0 | false |
| 4392 | Minotaur (#107) | D1002 (#17) / Whole Map (#268) | 1 | 222 | 0 | false |
| 4393 | Frost Minotaur (#108) | D1002 (#17) / Whole Map (#268) | 1 | 222 | 0 | false |
| 4394 | Shock Minotaur (#110) | D1002 (#17) / Whole Map (#268) | 1 | 222 | 0 | false |
| 4395 | Fury Minotaur (#112) | D1002 (#17) / Whole Map (#268) | 1 | 222 | 0 | false |
| 4396 | Flame Minotaur (#113) | D1002 (#17) / Whole Map (#268) | 1 | 222 | 0 | false |
| 4399 | Shock Minotaur (#110) | D1002 (#17) / Respawn Area 1 (#269) | 10 | 30 | 0 | false |
| 4406 | Flame Minotaur (#113) | D1002 (#17) / Respawn Area 2 (#270) | 10 | 30 | 0 | false |
| 4411 | Frost Minotaur (#108) | D1002 (#17) / Respawn Area 3 (#271) | 10 | 30 | 0 | false |
| 4416 | Fury Minotaur (#112) | D1002 (#17) / Respawn Area 4 (#272) | 10 | 30 | 0 | false |
| 4418 | Minotaur (#107) | D10031 (#18) / Whole Map (#279) | 1 | 200 | 0 | false |
| 4419 | Frost Minotaur (#108) | D10031 (#18) / Whole Map (#279) | 1 | 200 | 0 | false |
| 4420 | Shock Minotaur (#110) | D10031 (#18) / Whole Map (#279) | 1 | 266 | 0 | false |
| 4421 | Fury Minotaur (#112) | D10031 (#18) / Whole Map (#279) | 1 | 266 | 0 | false |
| 4422 | Flame Minotaur (#113) | D10031 (#18) / Whole Map (#279) | 1 | 266 | 0 | false |
| 4423 | Minotaur (#107) | D10031 (#18) / Respawn Area 1 (#280) | 10 | 4 | 0 | false |
| 4424 | Frost Minotaur (#108) | D10031 (#18) / Respawn Area 1 (#280) | 10 | 7 | 0 | false |
| 4425 | Shock Minotaur (#110) | D10031 (#18) / Respawn Area 1 (#280) | 10 | 7 | 0 | false |
| 4426 | Fury Minotaur (#112) | D10031 (#18) / Respawn Area 1 (#280) | 10 | 7 | 0 | false |
| 4427 | Flame Minotaur (#113) | D10031 (#18) / Respawn Area 1 (#280) | 10 | 7 | 0 | false |
| 4428 | Minotaur (#107) | D10031 (#18) / Respawn Area 2 (#281) | 10 | 4 | 0 | false |
| 4429 | Frost Minotaur (#108) | D10031 (#18) / Respawn Area 2 (#281) | 10 | 7 | 0 | false |
| 4430 | Shock Minotaur (#110) | D10031 (#18) / Respawn Area 2 (#281) | 10 | 7 | 0 | false |
| 4431 | Fury Minotaur (#112) | D10031 (#18) / Respawn Area 2 (#281) | 10 | 7 | 0 | false |
| 4432 | Flame Minotaur (#113) | D10031 (#18) / Respawn Area 2 (#281) | 10 | 7 | 0 | false |
| 4433 | Minotaur (#107) | D10031 (#18) / Respawn Area 3 (#282) | 10 | 4 | 0 | false |
| 4434 | Frost Minotaur (#108) | D10031 (#18) / Respawn Area 3 (#282) | 10 | 7 | 0 | false |
| 4435 | Shock Minotaur (#110) | D10031 (#18) / Respawn Area 3 (#282) | 10 | 7 | 0 | false |
| 4436 | Fury Minotaur (#112) | D10031 (#18) / Respawn Area 3 (#282) | 10 | 7 | 0 | false |
| 4437 | Flame Minotaur (#113) | D10031 (#18) / Respawn Area 3 (#282) | 10 | 7 | 0 | false |
| 4438 | Minotaur (#107) | D10031 (#18) / Respawn Area 4 (#283) | 10 | 4 | 0 | false |
| 4439 | Frost Minotaur (#108) | D10031 (#18) / Respawn Area 4 (#283) | 10 | 7 | 0 | false |
| 4440 | Shock Minotaur (#110) | D10031 (#18) / Respawn Area 4 (#283) | 10 | 7 | 0 | false |
| 4441 | Fury Minotaur (#112) | D10031 (#18) / Respawn Area 4 (#283) | 10 | 7 | 0 | false |
| 4442 | Flame Minotaur (#113) | D10031 (#18) / Respawn Area 4 (#283) | 10 | 7 | 0 | false |
| 4443 | Minotaur (#107) | D10031 (#18) / Respawn Area 5 (#284) | 10 | 4 | 0 | false |
| 4444 | Frost Minotaur (#108) | D10031 (#18) / Respawn Area 5 (#284) | 10 | 7 | 0 | false |
| 4445 | Shock Minotaur (#110) | D10031 (#18) / Respawn Area 5 (#284) | 10 | 7 | 0 | false |
| 4446 | Fury Minotaur (#112) | D10031 (#18) / Respawn Area 5 (#284) | 10 | 7 | 0 | false |
| 4447 | Flame Minotaur (#113) | D10031 (#18) / Respawn Area 5 (#284) | 10 | 7 | 0 | false |
| 4448 | Minotaur (#107) | D10031 (#18) / Respawn Area 6 (#285) | 10 | 8 | 0 | false |
| 4449 | Frost Minotaur (#108) | D10031 (#18) / Respawn Area 6 (#285) | 10 | 15 | 0 | false |
| 4450 | Shock Minotaur (#110) | D10031 (#18) / Respawn Area 6 (#285) | 10 | 15 | 0 | false |
| 4451 | Fury Minotaur (#112) | D10031 (#18) / Respawn Area 6 (#285) | 10 | 15 | 0 | false |
| 4452 | Flame Minotaur (#113) | D10031 (#18) / Respawn Area 6 (#285) | 10 | 15 | 0 | false |
| 4453 | Minotaur (#107) | D10031 (#18) / Respawn Area 7 (#288) | 10 | 2 | 0 | false |
| 4454 | Frost Minotaur (#108) | D10031 (#18) / Respawn Area 7 (#288) | 10 | 3 | 0 | false |
| 4455 | Shock Minotaur (#110) | D10031 (#18) / Respawn Area 7 (#288) | 10 | 3 | 0 | false |
| 4456 | Fury Minotaur (#112) | D10031 (#18) / Respawn Area 7 (#288) | 10 | 3 | 0 | false |
| 4457 | Flame Minotaur (#113) | D10031 (#18) / Respawn Area 7 (#288) | 10 | 3 | 0 | false |
| 4458 | Banya Left Guard (#111) | D10031 (#18) / Whole Map (#279) | 1 | 266 | 0 | false |
| 4459 | Banya Right Guard (#109) | D10031 (#18) / Whole Map (#279) | 1 | 155 | 0 | false |
| 4460 | Minotaur (#107) | D10032 (#19) / Whole Map (#289) | 10 | 10 | 0 | false |
| 4461 | Frost Minotaur (#108) | D10032 (#19) / Whole Map (#289) | 10 | 15 | 0 | false |
| 4462 | Shock Minotaur (#110) | D10032 (#19) / Whole Map (#289) | 10 | 25 | 0 | false |
| 4463 | Fury Minotaur (#112) | D10032 (#19) / Whole Map (#289) | 10 | 15 | 0 | false |
| 4464 | Flame Minotaur (#113) | D10032 (#19) / Whole Map (#289) | 10 | 15 | 0 | false |
| 4465 | Banya Left Guard (#111) | D10032 (#19) / Whole Map (#289) | 10 | 15 | 0 | false |
| 4466 | Banya Right Guard (#109) | D10032 (#19) / Whole Map (#289) | 10 | 15 | 0 | false |
| 4467 | Minotaur (#107) | D1004 (#20) / Whole Map (#294) | 5 | 20 | 0 | false |
| 4468 | Frost Minotaur (#108) | D1004 (#20) / Whole Map (#294) | 5 | 40 | 0 | false |
| 4469 | Shock Minotaur (#110) | D1004 (#20) / Whole Map (#294) | 5 | 40 | 0 | false |
| 4470 | Fury Minotaur (#112) | D1004 (#20) / Whole Map (#294) | 5 | 40 | 0 | false |
| 4471 | Flame Minotaur (#113) | D1004 (#20) / Whole Map (#294) | 5 | 40 | 0 | false |
| 4472 | Banya Left Guard (#111) | D1004 (#20) / Whole Map (#294) | 5 | 40 | 0 | false |
| 4473 | Banya Right Guard (#109) | D1004 (#20) / Whole Map (#294) | 5 | 40 | 0 | false |
| 4474 | Minotaur (#107) | D1004 (#20) / Respawn Area 1 (#299) | 15 | 5 | 0 | false |
| 4475 | Frost Minotaur (#108) | D1004 (#20) / Respawn Area 1 (#299) | 15 | 7 | 0 | false |
| 4476 | Shock Minotaur (#110) | D1004 (#20) / Respawn Area 1 (#299) | 15 | 7 | 0 | false |
| 4477 | Fury Minotaur (#112) | D1004 (#20) / Respawn Area 1 (#299) | 15 | 7 | 0 | false |
| 4478 | Flame Minotaur (#113) | D1004 (#20) / Respawn Area 1 (#299) | 15 | 7 | 0 | false |
| 4479 | Banya Left Guard (#111) | D1004 (#20) / Respawn Area 1 (#299) | 15 | 4 | 0 | false |
| 4480 | Banya Right Guard (#109) | D1004 (#20) / Respawn Area 1 (#299) | 15 | 4 | 0 | false |
| 4481 | Minotaur (#107) | D1004 (#20) / Respawn Area 2 (#300) | 15 | 5 | 0 | false |
| 4482 | Frost Minotaur (#108) | D1004 (#20) / Respawn Area 2 (#300) | 15 | 7 | 0 | false |
| 4483 | Shock Minotaur (#110) | D1004 (#20) / Respawn Area 2 (#300) | 15 | 7 | 0 | false |
| 4484 | Fury Minotaur (#112) | D1004 (#20) / Respawn Area 2 (#300) | 15 | 7 | 0 | false |
| 4485 | Flame Minotaur (#113) | D1004 (#20) / Respawn Area 2 (#300) | 15 | 7 | 0 | false |
| 4486 | Banya Left Guard (#111) | D1004 (#20) / Respawn Area 2 (#300) | 15 | 4 | 0 | false |
| 4487 | Banya Right Guard (#109) | D1004 (#20) / Respawn Area 2 (#300) | 15 | 4 | 0 | false |
| 4488 | Minotaur (#107) | D1005 (#21) / Whole Map (#301) | 1 | 55 | 0 | false |
| 4489 | Frost Minotaur (#108) | D1005 (#21) / Whole Map (#301) | 1 | 55 | 0 | false |
| 4490 | Shock Minotaur (#110) | D1005 (#21) / Whole Map (#301) | 1 | 77 | 0 | false |
| 4491 | Fury Minotaur (#112) | D1005 (#21) / Whole Map (#301) | 1 | 77 | 0 | false |
| 4492 | Flame Minotaur (#113) | D1005 (#21) / Whole Map (#301) | 1 | 60 | 0 | false |
| 4493 | Banya Left Guard (#111) | D1005 (#21) / Whole Map (#301) | 1 | 60 | 0 | false |

### #4167 · Chicken (#8) / 0 (#1) / Spawn Ring 1 (#31)

| 字段 | 值 |
|---|---|
| Monster | Chicken (#8) |
| Region | 0 (#1) / Spawn Ring 1 (#31) |
| EventSpawn | false |
| Delay | 1 |
| Count | 250 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4168 · Pig (#9) / 0 (#1) / Spawn Ring 1 (#31)

| 字段 | 值 |
|---|---|
| Monster | Pig (#9) |
| Region | 0 (#1) / Spawn Ring 1 (#31) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4169 · Cow (#11) / 0 (#1) / Spawn Ring 1 (#31)

| 字段 | 值 |
|---|---|
| Monster | Cow (#11) |
| Region | 0 (#1) / Spawn Ring 1 (#31) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4170 · Deer (#10) / 0 (#1) / Spawn Ring 1 (#31)

| 字段 | 值 |
|---|---|
| Monster | Deer (#10) |
| Region | 0 (#1) / Spawn Ring 1 (#31) |
| EventSpawn | false |
| Delay | 1 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4171 · Scarecrow (#21) / 0 (#1) / Spawn Ring 2 (#32)

| 字段 | 值 |
|---|---|
| Monster | Scarecrow (#21) |
| Region | 0 (#1) / Spawn Ring 2 (#32) |
| EventSpawn | false |
| Delay | 1 |
| Count | 400 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4172 · Claw Cat (#13) / 0 (#1) / Spawn Ring 2 (#32)

| 字段 | 值 |
|---|---|
| Monster | Claw Cat (#13) |
| Region | 0 (#1) / Spawn Ring 2 (#32) |
| EventSpawn | false |
| Delay | 1 |
| Count | 250 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4174 · Deer (#10) / 0 (#1) / Spawn Ring 2 (#32)

| 字段 | 值 |
|---|---|
| Monster | Deer (#10) |
| Region | 0 (#1) / Spawn Ring 2 (#32) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4175 · Oma (#22) / 0 (#1) / Spawn Ring 2 (#32)

| 字段 | 值 |
|---|---|
| Monster | Oma (#22) |
| Region | 0 (#1) / Spawn Ring 2 (#32) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4176 · Forest Yeti (#15) / 0 (#1) / Spawn Ring 2 (#32)

| 字段 | 值 |
|---|---|
| Monster | Forest Yeti (#15) |
| Region | 0 (#1) / Spawn Ring 2 (#32) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4179 · Tiger Snake (#19) / 0 (#1) / Spawn Ring 3 (#33)

| 字段 | 值 |
|---|---|
| Monster | Tiger Snake (#19) |
| Region | 0 (#1) / Spawn Ring 3 (#33) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4180 · Oma Warrior (#18) / 0 (#1) / Spawn Ring 3 (#33)

| 字段 | 值 |
|---|---|
| Monster | Oma Warrior (#18) |
| Region | 0 (#1) / Spawn Ring 3 (#33) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4181 · Spitting Spider (#20) / 0 (#1) / Spawn Ring 3 (#33)

| 字段 | 值 |
|---|---|
| Monster | Spitting Spider (#20) |
| Region | 0 (#1) / Spawn Ring 3 (#33) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4182 · Wolf (#14) / 0 (#1) / Spawn Ring 3 (#33)

| 字段 | 值 |
|---|---|
| Monster | Wolf (#14) |
| Region | 0 (#1) / Spawn Ring 3 (#33) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4183 · Oma Hero (#23) / 0 (#1) / Spawn Ring 3 (#33)

| 字段 | 值 |
|---|---|
| Monster | Oma Hero (#23) |
| Region | 0 (#1) / Spawn Ring 3 (#33) |
| EventSpawn | false |
| Delay | 30 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4184 · Chestnut Tree (#16) / 0 (#1) / Grass Area (#79)

| 字段 | 值 |
|---|---|
| Monster | Chestnut Tree (#16) |
| Region | 0 (#1) / Grass Area (#79) |
| EventSpawn | false |
| Delay | 30 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4185 · Carnivorous Plant (#17) / 0 (#1) / Grass Area (#79)

| 字段 | 值 |
|---|---|
| Monster | Carnivorous Plant (#17) |
| Region | 0 (#1) / Grass Area (#79) |
| EventSpawn | false |
| Delay | 1 |
| Count | 600 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4186 · Centipede (#51) / D801 (#160) / Whole Map (#427)

| 字段 | 值 |
|---|---|
| Monster | Centipede (#51) |
| Region | D801 (#160) / Whole Map (#427) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4187 · Butterfly Worm (#52) / D801 (#160) / Whole Map (#427)

| 字段 | 值 |
|---|---|
| Monster | Butterfly Worm (#52) |
| Region | D801 (#160) / Whole Map (#427) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4188 · Wasp Hatchling (#50) / D801 (#160) / Whole Map (#427)

| 字段 | 值 |
|---|---|
| Monster | Wasp Hatchling (#50) |
| Region | D801 (#160) / Whole Map (#427) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4189 · Mutant Maggot (#53) / D801 (#160) / Whole Map (#427)

| 字段 | 值 |
|---|---|
| Monster | Mutant Maggot (#53) |
| Region | D801 (#160) / Whole Map (#427) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4190 · Centipede (#51) / D801 (#160) / Respawn Area 1 (#434)

| 字段 | 值 |
|---|---|
| Monster | Centipede (#51) |
| Region | D801 (#160) / Respawn Area 1 (#434) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4191 · Butterfly Worm (#52) / D801 (#160) / Respawn Area 1 (#434)

| 字段 | 值 |
|---|---|
| Monster | Butterfly Worm (#52) |
| Region | D801 (#160) / Respawn Area 1 (#434) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4192 · Wasp Hatchling (#50) / D801 (#160) / Respawn Area 1 (#434)

| 字段 | 值 |
|---|---|
| Monster | Wasp Hatchling (#50) |
| Region | D801 (#160) / Respawn Area 1 (#434) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4193 · Mutant Maggot (#53) / D801 (#160) / Respawn Area 1 (#434)

| 字段 | 值 |
|---|---|
| Monster | Mutant Maggot (#53) |
| Region | D801 (#160) / Respawn Area 1 (#434) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4194 · Centipede (#51) / D802 (#161) / Whole Map (#435)

| 字段 | 值 |
|---|---|
| Monster | Centipede (#51) |
| Region | D802 (#161) / Whole Map (#435) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4195 · Butterfly Worm (#52) / D802 (#161) / Whole Map (#435)

| 字段 | 值 |
|---|---|
| Monster | Butterfly Worm (#52) |
| Region | D802 (#161) / Whole Map (#435) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4196 · Wasp Hatchling (#50) / D802 (#161) / Whole Map (#435)

| 字段 | 值 |
|---|---|
| Monster | Wasp Hatchling (#50) |
| Region | D802 (#161) / Whole Map (#435) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4197 · Mutant Maggot (#53) / D802 (#161) / Whole Map (#435)

| 字段 | 值 |
|---|---|
| Monster | Mutant Maggot (#53) |
| Region | D802 (#161) / Whole Map (#435) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4198 · Centipede (#51) / D803 (#162) / Whole Map (#444)

| 字段 | 值 |
|---|---|
| Monster | Centipede (#51) |
| Region | D803 (#162) / Whole Map (#444) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4199 · Butterfly Worm (#52) / D803 (#162) / Whole Map (#444)

| 字段 | 值 |
|---|---|
| Monster | Butterfly Worm (#52) |
| Region | D803 (#162) / Whole Map (#444) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4200 · Wasp Hatchling (#50) / D803 (#162) / Whole Map (#444)

| 字段 | 值 |
|---|---|
| Monster | Wasp Hatchling (#50) |
| Region | D803 (#162) / Whole Map (#444) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4201 · Mutant Maggot (#53) / D803 (#162) / Whole Map (#444)

| 字段 | 值 |
|---|---|
| Monster | Mutant Maggot (#53) |
| Region | D803 (#162) / Whole Map (#444) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4202 · Earwig (#54) / D803 (#162) / Whole Map (#444)

| 字段 | 值 |
|---|---|
| Monster | Earwig (#54) |
| Region | D803 (#162) / Whole Map (#444) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4203 · Centipede (#51) / D803 (#162) / Respawn Area 1 (#451)

| 字段 | 值 |
|---|---|
| Monster | Centipede (#51) |
| Region | D803 (#162) / Respawn Area 1 (#451) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4204 · Butterfly Worm (#52) / D803 (#162) / Respawn Area 1 (#451)

| 字段 | 值 |
|---|---|
| Monster | Butterfly Worm (#52) |
| Region | D803 (#162) / Respawn Area 1 (#451) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4205 · Wasp Hatchling (#50) / D803 (#162) / Respawn Area 1 (#451)

| 字段 | 值 |
|---|---|
| Monster | Wasp Hatchling (#50) |
| Region | D803 (#162) / Respawn Area 1 (#451) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4206 · Mutant Maggot (#53) / D803 (#162) / Respawn Area 1 (#451)

| 字段 | 值 |
|---|---|
| Monster | Mutant Maggot (#53) |
| Region | D803 (#162) / Respawn Area 1 (#451) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4207 · Earwig (#54) / D803 (#162) / Respawn Area 1 (#451)

| 字段 | 值 |
|---|---|
| Monster | Earwig (#54) |
| Region | D803 (#162) / Respawn Area 1 (#451) |
| EventSpawn | false |
| Delay | 15 |
| Count | 5 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4208 · Centipede (#51) / D804 (#163) / Whole Map (#452)

| 字段 | 值 |
|---|---|
| Monster | Centipede (#51) |
| Region | D804 (#163) / Whole Map (#452) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4209 · Butterfly Worm (#52) / D804 (#163) / Whole Map (#452)

| 字段 | 值 |
|---|---|
| Monster | Butterfly Worm (#52) |
| Region | D804 (#163) / Whole Map (#452) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4210 · Wasp Hatchling (#50) / D804 (#163) / Whole Map (#452)

| 字段 | 值 |
|---|---|
| Monster | Wasp Hatchling (#50) |
| Region | D804 (#163) / Whole Map (#452) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4211 · Mutant Maggot (#53) / D804 (#163) / Whole Map (#452)

| 字段 | 值 |
|---|---|
| Monster | Mutant Maggot (#53) |
| Region | D804 (#163) / Whole Map (#452) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4212 · Earwig (#54) / D804 (#163) / Whole Map (#452)

| 字段 | 值 |
|---|---|
| Monster | Earwig (#54) |
| Region | D804 (#163) / Whole Map (#452) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4213 · Centipede (#51) / D804 (#163) / Respawn Area 1 (#457)

| 字段 | 值 |
|---|---|
| Monster | Centipede (#51) |
| Region | D804 (#163) / Respawn Area 1 (#457) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4214 · Butterfly Worm (#52) / D804 (#163) / Respawn Area 1 (#457)

| 字段 | 值 |
|---|---|
| Monster | Butterfly Worm (#52) |
| Region | D804 (#163) / Respawn Area 1 (#457) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4215 · Wasp Hatchling (#50) / D804 (#163) / Respawn Area 1 (#457)

| 字段 | 值 |
|---|---|
| Monster | Wasp Hatchling (#50) |
| Region | D804 (#163) / Respawn Area 1 (#457) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4216 · Mutant Maggot (#53) / D804 (#163) / Respawn Area 1 (#457)

| 字段 | 值 |
|---|---|
| Monster | Mutant Maggot (#53) |
| Region | D804 (#163) / Respawn Area 1 (#457) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4217 · Earwig (#54) / D804 (#163) / Respawn Area 1 (#457)

| 字段 | 值 |
|---|---|
| Monster | Earwig (#54) |
| Region | D804 (#163) / Respawn Area 1 (#457) |
| EventSpawn | false |
| Delay | 15 |
| Count | 5 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4218 · Centipede (#51) / D804 (#163) / Respawn Area 2 (#458)

| 字段 | 值 |
|---|---|
| Monster | Centipede (#51) |
| Region | D804 (#163) / Respawn Area 2 (#458) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4219 · Butterfly Worm (#52) / D804 (#163) / Respawn Area 2 (#458)

| 字段 | 值 |
|---|---|
| Monster | Butterfly Worm (#52) |
| Region | D804 (#163) / Respawn Area 2 (#458) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4220 · Wasp Hatchling (#50) / D804 (#163) / Respawn Area 2 (#458)

| 字段 | 值 |
|---|---|
| Monster | Wasp Hatchling (#50) |
| Region | D804 (#163) / Respawn Area 2 (#458) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4221 · Mutant Maggot (#53) / D804 (#163) / Respawn Area 2 (#458)

| 字段 | 值 |
|---|---|
| Monster | Mutant Maggot (#53) |
| Region | D804 (#163) / Respawn Area 2 (#458) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4222 · Earwig (#54) / D804 (#163) / Respawn Area 2 (#458)

| 字段 | 值 |
|---|---|
| Monster | Earwig (#54) |
| Region | D804 (#163) / Respawn Area 2 (#458) |
| EventSpawn | false |
| Delay | 15 |
| Count | 5 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4223 · Centipede (#51) / D805 (#164) / Whole Map (#90)

| 字段 | 值 |
|---|---|
| Monster | Centipede (#51) |
| Region | D805 (#164) / Whole Map (#90) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4224 · Butterfly Worm (#52) / D805 (#164) / Whole Map (#90)

| 字段 | 值 |
|---|---|
| Monster | Butterfly Worm (#52) |
| Region | D805 (#164) / Whole Map (#90) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4225 · Wasp Hatchling (#50) / D805 (#164) / Whole Map (#90)

| 字段 | 值 |
|---|---|
| Monster | Wasp Hatchling (#50) |
| Region | D805 (#164) / Whole Map (#90) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4226 · Mutant Maggot (#53) / D805 (#164) / Whole Map (#90)

| 字段 | 值 |
|---|---|
| Monster | Mutant Maggot (#53) |
| Region | D805 (#164) / Whole Map (#90) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4227 · Earwig (#54) / D805 (#164) / Whole Map (#90)

| 字段 | 值 |
|---|---|
| Monster | Earwig (#54) |
| Region | D805 (#164) / Whole Map (#90) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4228 · Centipede (#51) / D805 (#164) / Whole Map (#90)

| 字段 | 值 |
|---|---|
| Monster | Centipede (#51) |
| Region | D805 (#164) / Whole Map (#90) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4229 · Butterfly Worm (#52) / D805 (#164) / Respawn Area  (#463)

| 字段 | 值 |
|---|---|
| Monster | Butterfly Worm (#52) |
| Region | D805 (#164) / Respawn Area  (#463) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4230 · Wasp Hatchling (#50) / D805 (#164) / Respawn Area  (#463)

| 字段 | 值 |
|---|---|
| Monster | Wasp Hatchling (#50) |
| Region | D805 (#164) / Respawn Area  (#463) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4231 · Mutant Maggot (#53) / D805 (#164) / Respawn Area  (#463)

| 字段 | 值 |
|---|---|
| Monster | Mutant Maggot (#53) |
| Region | D805 (#164) / Respawn Area  (#463) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4232 · Earwig (#54) / D805 (#164) / Respawn Area  (#463)

| 字段 | 值 |
|---|---|
| Monster | Earwig (#54) |
| Region | D805 (#164) / Respawn Area  (#463) |
| EventSpawn | false |
| Delay | 15 |
| Count | 5 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4233 · Centipede (#51) / D805 (#164) / Lord Ji'Nae Area (#462)

| 字段 | 值 |
|---|---|
| Monster | Centipede (#51) |
| Region | D805 (#164) / Lord Ji'Nae Area (#462) |
| EventSpawn | false |
| Delay | 5 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4234 · Earwig (#54) / D805 (#164) / Lord Ji'Nae Area (#462)

| 字段 | 值 |
|---|---|
| Monster | Earwig (#54) |
| Region | D805 (#164) / Lord Ji'Nae Area (#462) |
| EventSpawn | false |
| Delay | 5 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4235 · Lord Ji'Nae (#56) / D805 (#164) / Lord Ji'Nae (#461)

| 字段 | 值 |
|---|---|
| Monster | Lord Ji'Nae (#56) |
| Region | D805 (#164) / Lord Ji'Nae (#461) |
| EventSpawn | false |
| Delay | 300 |
| Count | 1 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4236 · Iron Lance (#55) / D805 (#164) / Whole Map (#90)

| 字段 | 值 |
|---|---|
| Monster | Iron Lance (#55) |
| Region | D805 (#164) / Whole Map (#90) |
| EventSpawn | false |
| Delay | 30 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4237 · Ant Soldier (#38) / D401 (#142) / Whole Map (#464)

| 字段 | 值 |
|---|---|
| Monster | Ant Soldier (#38) |
| Region | D401 (#142) / Whole Map (#464) |
| EventSpawn | false |
| Delay | 1 |
| Count | 160 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4238 · Ant Needler (#40) / D401 (#142) / Whole Map (#464)

| 字段 | 值 |
|---|---|
| Monster | Ant Needler (#40) |
| Region | D401 (#142) / Whole Map (#464) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4239 · Armoured Ant (#41) / D402 (#143) / Whole Map (#469)

| 字段 | 值 |
|---|---|
| Monster | Armoured Ant (#41) |
| Region | D402 (#143) / Whole Map (#469) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4240 · Ant Soldier (#38) / D402 (#143) / Whole Map (#469)

| 字段 | 值 |
|---|---|
| Monster | Ant Soldier (#38) |
| Region | D402 (#143) / Whole Map (#469) |
| EventSpawn | false |
| Delay | 1 |
| Count | 240 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4241 · Ant Needler (#40) / D402 (#143) / Whole Map (#469)

| 字段 | 值 |
|---|---|
| Monster | Ant Needler (#40) |
| Region | D402 (#143) / Whole Map (#469) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4242 · Armoured Ant (#41) / D403 (#144) / Whole Map (#478)

| 字段 | 值 |
|---|---|
| Monster | Armoured Ant (#41) |
| Region | D403 (#144) / Whole Map (#478) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4243 · Ant Soldier (#38) / D403 (#144) / Whole Map (#478)

| 字段 | 值 |
|---|---|
| Monster | Ant Soldier (#38) |
| Region | D403 (#144) / Whole Map (#478) |
| EventSpawn | false |
| Delay | 1 |
| Count | 180 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4244 · Ant Needler (#40) / D403 (#144) / Whole Map (#478)

| 字段 | 值 |
|---|---|
| Monster | Ant Needler (#40) |
| Region | D403 (#144) / Whole Map (#478) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4245 · Ant Healer (#39) / D403 (#144) / Whole Map (#478)

| 字段 | 值 |
|---|---|
| Monster | Ant Healer (#39) |
| Region | D403 (#144) / Whole Map (#478) |
| EventSpawn | false |
| Delay | 1 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4246 · Armoured Ant (#41) / D404 (#145) / Whole Map (#490)

| 字段 | 值 |
|---|---|
| Monster | Armoured Ant (#41) |
| Region | D404 (#145) / Whole Map (#490) |
| EventSpawn | false |
| Delay | 1 |
| Count | 250 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4247 · Ant Soldier (#38) / D404 (#145) / Whole Map (#490)

| 字段 | 值 |
|---|---|
| Monster | Ant Soldier (#38) |
| Region | D404 (#145) / Whole Map (#490) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4248 · Ant Needler (#40) / D404 (#145) / Whole Map (#490)

| 字段 | 值 |
|---|---|
| Monster | Ant Needler (#40) |
| Region | D404 (#145) / Whole Map (#490) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4249 · Ant Healer (#39) / D404 (#145) / Whole Map (#490)

| 字段 | 值 |
|---|---|
| Monster | Ant Healer (#39) |
| Region | D404 (#145) / Whole Map (#490) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4250 · Ant Commander (#42) / D404 (#145) / Whole Map (#490)

| 字段 | 值 |
|---|---|
| Monster | Ant Commander (#42) |
| Region | D404 (#145) / Whole Map (#490) |
| EventSpawn | false |
| Delay | 30 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4252 · Skeleton (#27) / D101 (#26) / Whole Map (#99)

| 字段 | 值 |
|---|---|
| Monster | Skeleton (#27) |
| Region | D101 (#26) / Whole Map (#99) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4253 · Cave Bat (#24) / D101 (#26) / Whole Map (#99)

| 字段 | 值 |
|---|---|
| Monster | Cave Bat (#24) |
| Region | D101 (#26) / Whole Map (#99) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4254 · Scorpion (#25) / D101 (#26) / Whole Map (#99)

| 字段 | 值 |
|---|---|
| Monster | Scorpion (#25) |
| Region | D101 (#26) / Whole Map (#99) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4256 · Skeleton (#27) / D102 (#31) / Whole Map (#369)

| 字段 | 值 |
|---|---|
| Monster | Skeleton (#27) |
| Region | D102 (#31) / Whole Map (#369) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4257 · Cave Bat (#24) / D102 (#31) / Whole Map (#369)

| 字段 | 值 |
|---|---|
| Monster | Cave Bat (#24) |
| Region | D102 (#31) / Whole Map (#369) |
| EventSpawn | false |
| Delay | 1 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4258 · Scorpion (#25) / D102 (#31) / Whole Map (#369)

| 字段 | 值 |
|---|---|
| Monster | Scorpion (#25) |
| Region | D102 (#31) / Whole Map (#369) |
| EventSpawn | false |
| Delay | 1 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4259 · Skeleton Axe Thrower (#28) / D102 (#31) / Whole Map (#369)

| 字段 | 值 |
|---|---|
| Monster | Skeleton Axe Thrower (#28) |
| Region | D102 (#31) / Whole Map (#369) |
| EventSpawn | false |
| Delay | 1 |
| Count | 65 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4261 · Skeleton (#27) / D103 (#32) / Whole Map (#378)

| 字段 | 值 |
|---|---|
| Monster | Skeleton (#27) |
| Region | D103 (#32) / Whole Map (#378) |
| EventSpawn | false |
| Delay | 1 |
| Count | 180 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4262 · Cave Bat (#24) / D103 (#32) / Whole Map (#378)

| 字段 | 值 |
|---|---|
| Monster | Cave Bat (#24) |
| Region | D103 (#32) / Whole Map (#378) |
| EventSpawn | false |
| Delay | 1 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4263 · Skeleton Warrior (#29) / D103 (#32) / Whole Map (#378)

| 字段 | 值 |
|---|---|
| Monster | Skeleton Warrior (#29) |
| Region | D103 (#32) / Whole Map (#378) |
| EventSpawn | false |
| Delay | 1 |
| Count | 180 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4264 · Skeleton Axe Thrower (#28) / D103 (#32) / Whole Map (#378)

| 字段 | 值 |
|---|---|
| Monster | Skeleton Axe Thrower (#28) |
| Region | D103 (#32) / Whole Map (#378) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4265 · Skeleton Lord (#30) / D103 (#32) / Whole Map (#378)

| 字段 | 值 |
|---|---|
| Monster | Skeleton Lord (#30) |
| Region | D103 (#32) / Whole Map (#378) |
| EventSpawn | false |
| Delay | 30 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4266 · Cave Maggot (#31) / D201 (#136) / Whole Map (#498)

| 字段 | 值 |
|---|---|
| Monster | Cave Maggot (#31) |
| Region | D201 (#136) / Whole Map (#498) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4267 · GhostSorcerer (#32) / D201 (#136) / Whole Map (#498)

| 字段 | 值 |
|---|---|
| Monster | GhostSorcerer (#32) |
| Region | D201 (#136) / Whole Map (#498) |
| EventSpawn | false |
| Delay | 1 |
| Count | 40 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4268 · Ghost Mage (#33) / D201 (#136) / Whole Map (#498)

| 字段 | 值 |
|---|---|
| Monster | Ghost Mage (#33) |
| Region | D201 (#136) / Whole Map (#498) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4269 · Devouring Ghost (#35) / D201 (#136) / Whole Map (#498)

| 字段 | 值 |
|---|---|
| Monster | Devouring Ghost (#35) |
| Region | D201 (#136) / Whole Map (#498) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4270 · Corpse Raising Ghost (#36) / D201 (#136) / Whole Map (#498)

| 字段 | 值 |
|---|---|
| Monster | Corpse Raising Ghost (#36) |
| Region | D201 (#136) / Whole Map (#498) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4271 · Voracious Ghost (#34) / D201 (#136) / Whole Map (#498)

| 字段 | 值 |
|---|---|
| Monster | Voracious Ghost (#34) |
| Region | D201 (#136) / Whole Map (#498) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 2 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4272 · Cave Maggot (#31) / D202 (#137) / Whole Map (#507)

| 字段 | 值 |
|---|---|
| Monster | Cave Maggot (#31) |
| Region | D202 (#137) / Whole Map (#507) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4273 · GhostSorcerer (#32) / D202 (#137) / Whole Map (#507)

| 字段 | 值 |
|---|---|
| Monster | GhostSorcerer (#32) |
| Region | D202 (#137) / Whole Map (#507) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 4 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4274 · Ghost Mage (#33) / D202 (#137) / Whole Map (#507)

| 字段 | 值 |
|---|---|
| Monster | Ghost Mage (#33) |
| Region | D202 (#137) / Whole Map (#507) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 4 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4275 · Devouring Ghost (#35) / D202 (#137) / Whole Map (#507)

| 字段 | 值 |
|---|---|
| Monster | Devouring Ghost (#35) |
| Region | D202 (#137) / Whole Map (#507) |
| EventSpawn | false |
| Delay | 1 |
| Count | 175 |
| DropSet | 4 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4276 · Corpse Raising Ghost (#36) / D202 (#137) / Whole Map (#507)

| 字段 | 值 |
|---|---|
| Monster | Corpse Raising Ghost (#36) |
| Region | D202 (#137) / Whole Map (#507) |
| EventSpawn | false |
| Delay | 1 |
| Count | 175 |
| DropSet | 4 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4277 · Voracious Ghost (#34) / D202 (#137) / Whole Map (#507)

| 字段 | 值 |
|---|---|
| Monster | Voracious Ghost (#34) |
| Region | D202 (#137) / Whole Map (#507) |
| EventSpawn | false |
| Delay | 1 |
| Count | 175 |
| DropSet | 4 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4278 · Cave Maggot (#31) / D203 (#138) / Whole Map (#515)

| 字段 | 值 |
|---|---|
| Monster | Cave Maggot (#31) |
| Region | D203 (#138) / Whole Map (#515) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4279 · GhostSorcerer (#32) / D203 (#138) / Whole Map (#515)

| 字段 | 值 |
|---|---|
| Monster | GhostSorcerer (#32) |
| Region | D203 (#138) / Whole Map (#515) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4280 · Ghost Mage (#33) / D203 (#138) / Whole Map (#515)

| 字段 | 值 |
|---|---|
| Monster | Ghost Mage (#33) |
| Region | D203 (#138) / Whole Map (#515) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4281 · Devouring Ghost (#35) / D203 (#138) / Whole Map (#515)

| 字段 | 值 |
|---|---|
| Monster | Devouring Ghost (#35) |
| Region | D203 (#138) / Whole Map (#515) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4282 · Corpse Raising Ghost (#36) / D203 (#138) / Whole Map (#515)

| 字段 | 值 |
|---|---|
| Monster | Corpse Raising Ghost (#36) |
| Region | D203 (#138) / Whole Map (#515) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4283 · Voracious Ghost (#34) / D203 (#138) / Whole Map (#515)

| 字段 | 值 |
|---|---|
| Monster | Voracious Ghost (#34) |
| Region | D203 (#138) / Whole Map (#515) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4284 · Ghoul Champion (#37) / D203 (#138) / Whole Map (#515)

| 字段 | 值 |
|---|---|
| Monster | Ghoul Champion (#37) |
| Region | D203 (#138) / Whole Map (#515) |
| EventSpawn | false |
| Delay | 30 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4285 · Dark Arachnid (#72) / D001 (#12) / Whole Map (#229)

| 字段 | 值 |
|---|---|
| Monster | Dark Arachnid (#72) |
| Region | D001 (#12) / Whole Map (#229) |
| EventSpawn | false |
| Delay | 1 |
| Count | 225 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4286 · Spider Bat (#66) / D001 (#12) / Whole Map (#229)

| 字段 | 值 |
|---|---|
| Monster | Spider Bat (#66) |
| Region | D001 (#12) / Whole Map (#229) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4287 · Arachnid Gazer (#67) / D001 (#12) / Whole Map (#229)

| 字段 | 值 |
|---|---|
| Monster | Arachnid Gazer (#67) |
| Region | D001 (#12) / Whole Map (#229) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4288 · Venomous Arachnid (#71) / D001 (#12) / Whole Map (#229)

| 字段 | 值 |
|---|---|
| Monster | Venomous Arachnid (#71) |
| Region | D001 (#12) / Whole Map (#229) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4289 · Arachnid Broodmother (#73) / D001 (#12) / Respawn Areas (#230)

| 字段 | 值 |
|---|---|
| Monster | Arachnid Broodmother (#73) |
| Region | D001 (#12) / Respawn Areas (#230) |
| EventSpawn | false |
| Delay | 15 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4291 · Dark Arachnid (#72) / D001 (#12) / Respawn Areas (#230)

| 字段 | 值 |
|---|---|
| Monster | Dark Arachnid (#72) |
| Region | D001 (#12) / Respawn Areas (#230) |
| EventSpawn | false |
| Delay | 15 |
| Count | 150 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4292 · Spider Bat (#66) / D001 (#12) / Respawn Areas (#230)

| 字段 | 值 |
|---|---|
| Monster | Spider Bat (#66) |
| Region | D001 (#12) / Respawn Areas (#230) |
| EventSpawn | false |
| Delay | 15 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4293 · Arachnid Gazer (#67) / D001 (#12) / Respawn Areas (#230)

| 字段 | 值 |
|---|---|
| Monster | Arachnid Gazer (#67) |
| Region | D001 (#12) / Respawn Areas (#230) |
| EventSpawn | false |
| Delay | 15 |
| Count | 5 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4294 · Venomous Arachnid (#71) / D001 (#12) / Respawn Areas (#230)

| 字段 | 值 |
|---|---|
| Monster | Venomous Arachnid (#71) |
| Region | D001 (#12) / Respawn Areas (#230) |
| EventSpawn | false |
| Delay | 15 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4295 · Dark Arachnid (#72) / D901 (#165) / Whole Map (#596)

| 字段 | 值 |
|---|---|
| Monster | Dark Arachnid (#72) |
| Region | D901 (#165) / Whole Map (#596) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4296 · Spider Bat (#66) / D901 (#165) / Whole Map (#596)

| 字段 | 值 |
|---|---|
| Monster | Spider Bat (#66) |
| Region | D901 (#165) / Whole Map (#596) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4297 · Arachnid Gazer (#67) / D901 (#165) / Whole Map (#596)

| 字段 | 值 |
|---|---|
| Monster | Arachnid Gazer (#67) |
| Region | D901 (#165) / Whole Map (#596) |
| EventSpawn | false |
| Delay | 1 |
| Count | 20 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4298 · Venomous Arachnid (#71) / D901 (#165) / Whole Map (#596)

| 字段 | 值 |
|---|---|
| Monster | Venomous Arachnid (#71) |
| Region | D901 (#165) / Whole Map (#596) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4299 · Dark Arachnid (#72) / D902 (#166) / Whole Map (#605)

| 字段 | 值 |
|---|---|
| Monster | Dark Arachnid (#72) |
| Region | D902 (#166) / Whole Map (#605) |
| EventSpawn | false |
| Delay | 1 |
| Count | 125 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4300 · Spider Bat (#66) / D902 (#166) / Whole Map (#605)

| 字段 | 值 |
|---|---|
| Monster | Spider Bat (#66) |
| Region | D902 (#166) / Whole Map (#605) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4301 · Arachnid Gazer (#67) / D902 (#166) / Whole Map (#605)

| 字段 | 值 |
|---|---|
| Monster | Arachnid Gazer (#67) |
| Region | D902 (#166) / Whole Map (#605) |
| EventSpawn | false |
| Delay | 1 |
| Count | 10 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4302 · Venomous Arachnid (#71) / D902 (#166) / Whole Map (#605)

| 字段 | 值 |
|---|---|
| Monster | Venomous Arachnid (#71) |
| Region | D902 (#166) / Whole Map (#605) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4303 · Red Moon Guardian (#69) / D902 (#166) / Whole Map (#605)

| 字段 | 值 |
|---|---|
| Monster | Red Moon Guardian (#69) |
| Region | D902 (#166) / Whole Map (#605) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4304 · Dark Arachnid (#72) / D903 (#167) / Whole Map (#614)

| 字段 | 值 |
|---|---|
| Monster | Dark Arachnid (#72) |
| Region | D903 (#167) / Whole Map (#614) |
| EventSpawn | false |
| Delay | 1 |
| Count | 150 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4305 · Spider Bat (#66) / D903 (#167) / Whole Map (#614)

| 字段 | 值 |
|---|---|
| Monster | Spider Bat (#66) |
| Region | D903 (#167) / Whole Map (#614) |
| EventSpawn | false |
| Delay | 1 |
| Count | 80 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4306 · Arachnid Gazer (#67) / D903 (#167) / Whole Map (#614)

| 字段 | 值 |
|---|---|
| Monster | Arachnid Gazer (#67) |
| Region | D903 (#167) / Whole Map (#614) |
| EventSpawn | false |
| Delay | 1 |
| Count | 20 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4307 · Venomous Arachnid (#71) / D903 (#167) / Whole Map (#614)

| 字段 | 值 |
|---|---|
| Monster | Venomous Arachnid (#71) |
| Region | D903 (#167) / Whole Map (#614) |
| EventSpawn | false |
| Delay | 1 |
| Count | 75 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4308 · Red Moon Guardian (#69) / D903 (#167) / Whole Map (#614)

| 字段 | 值 |
|---|---|
| Monster | Red Moon Guardian (#69) |
| Region | D903 (#167) / Whole Map (#614) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4309 · Red Moon Protector (#70) / D903 (#167) / Whole Map (#614)

| 字段 | 值 |
|---|---|
| Monster | Red Moon Protector (#70) |
| Region | D903 (#167) / Whole Map (#614) |
| EventSpawn | false |
| Delay | 1 |
| Count | 120 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4310 · Dark Arachnid (#72) / D904 (#168) / Whole Map (#621)

| 字段 | 值 |
|---|---|
| Monster | Dark Arachnid (#72) |
| Region | D904 (#168) / Whole Map (#621) |
| EventSpawn | false |
| Delay | 1 |
| Count | 255 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4311 · Spider Bat (#66) / D904 (#168) / Whole Map (#621)

| 字段 | 值 |
|---|---|
| Monster | Spider Bat (#66) |
| Region | D904 (#168) / Whole Map (#621) |
| EventSpawn | false |
| Delay | 1 |
| Count | 255 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4312 · Arachnid Gazer (#67) / D904 (#168) / Whole Map (#621)

| 字段 | 值 |
|---|---|
| Monster | Arachnid Gazer (#67) |
| Region | D904 (#168) / Whole Map (#621) |
| EventSpawn | false |
| Delay | 1 |
| Count | 70 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4313 · Venomous Arachnid (#71) / D904 (#168) / Whole Map (#621)

| 字段 | 值 |
|---|---|
| Monster | Venomous Arachnid (#71) |
| Region | D904 (#168) / Whole Map (#621) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4314 · Red Moon Guardian (#69) / D904 (#168) / Whole Map (#621)

| 字段 | 值 |
|---|---|
| Monster | Red Moon Guardian (#69) |
| Region | D904 (#168) / Whole Map (#621) |
| EventSpawn | false |
| Delay | 1 |
| Count | 350 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4315 · Red Moon Protector (#70) / D904 (#168) / Whole Map (#621)

| 字段 | 值 |
|---|---|
| Monster | Red Moon Protector (#70) |
| Region | D904 (#168) / Whole Map (#621) |
| EventSpawn | false |
| Delay | 1 |
| Count | 250 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4316 · Red Moon Royal Guard (#74) / D904 (#168) / Whole Map (#621)

| 字段 | 值 |
|---|---|
| Monster | Red Moon Royal Guard (#74) |
| Region | D904 (#168) / Whole Map (#621) |
| EventSpawn | false |
| Delay | 30 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4317 · Dark Arachnid (#72) / D905 (#559) / Whole Map (#628)

| 字段 | 值 |
|---|---|
| Monster | Dark Arachnid (#72) |
| Region | D905 (#559) / Whole Map (#628) |
| EventSpawn | false |
| Delay | 5 |
| Count | 7 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4318 · Spider Bat (#66) / D905 (#559) / Whole Map (#628)

| 字段 | 值 |
|---|---|
| Monster | Spider Bat (#66) |
| Region | D905 (#559) / Whole Map (#628) |
| EventSpawn | false |
| Delay | 5 |
| Count | 7 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4319 · Arachnid Gazer (#67) / D905 (#559) / Whole Map (#628)

| 字段 | 值 |
|---|---|
| Monster | Arachnid Gazer (#67) |
| Region | D905 (#559) / Whole Map (#628) |
| EventSpawn | false |
| Delay | 5 |
| Count | 7 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4320 · Venomous Arachnid (#71) / D905 (#559) / Whole Map (#628)

| 字段 | 值 |
|---|---|
| Monster | Venomous Arachnid (#71) |
| Region | D905 (#559) / Whole Map (#628) |
| EventSpawn | false |
| Delay | 5 |
| Count | 7 |
| DropSet | 1 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4321 · Red Moon Guardian (#69) / D905 (#559) / Whole Map (#628)

| 字段 | 值 |
|---|---|
| Monster | Red Moon Guardian (#69) |
| Region | D905 (#559) / Whole Map (#628) |
| EventSpawn | false |
| Delay | 5 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4322 · Red Moon Protector (#70) / D905 (#559) / Whole Map (#628)

| 字段 | 值 |
|---|---|
| Monster | Red Moon Protector (#70) |
| Region | D905 (#559) / Whole Map (#628) |
| EventSpawn | false |
| Delay | 5 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4323 · Red Moon The Fallen (#75) / D905 (#559) / Red Moon (#710)

| 字段 | 值 |
|---|---|
| Monster | Red Moon The Fallen (#75) |
| Region | D905 (#559) / Red Moon (#710) |
| EventSpawn | true |
| Delay | 300 |
| Count | 1 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4324 · Vicious Rat (#79) / D1101 (#33) / Whole Map (#387)

| 字段 | 值 |
|---|---|
| Monster | Vicious Rat (#79) |
| Region | D1101 (#33) / Whole Map (#387) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4325 · Zuma Sharpshooter (#76) / D1101 (#33) / Whole Map (#387)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1101 (#33) / Whole Map (#387) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4326 · Vicious Rat (#79) / D1101 (#33) / Respawn Area 2 (#393)

| 字段 | 值 |
|---|---|
| Monster | Vicious Rat (#79) |
| Region | D1101 (#33) / Respawn Area 2 (#393) |
| EventSpawn | false |
| Delay | 15 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4327 · Zuma Sharpshooter (#76) / D1101 (#33) / Respawn Area 2 (#393)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1101 (#33) / Respawn Area 2 (#393) |
| EventSpawn | false |
| Delay | 15 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4328 · Vicious Rat (#79) / D1102 (#34) / Whole Map (#394)

| 字段 | 值 |
|---|---|
| Monster | Vicious Rat (#79) |
| Region | D1102 (#34) / Whole Map (#394) |
| EventSpawn | false |
| Delay | 1 |
| Count | 350 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4329 · Zuma Sharpshooter (#76) / D1102 (#34) / Whole Map (#394)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1102 (#34) / Whole Map (#394) |
| EventSpawn | false |
| Delay | 1 |
| Count | 160 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4330 · Vicious Rat (#79) / D1103 (#35) / Whole Map (#401)

| 字段 | 值 |
|---|---|
| Monster | Vicious Rat (#79) |
| Region | D1103 (#35) / Whole Map (#401) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4331 · Zuma Sharpshooter (#76) / D1103 (#35) / Whole Map (#401)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1103 (#35) / Whole Map (#401) |
| EventSpawn | false |
| Delay | 1 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4332 · Zuma Fanatic (#77) / D1103 (#35) / Whole Map (#401)

| 字段 | 值 |
|---|---|
| Monster | Zuma Fanatic (#77) |
| Region | D1103 (#35) / Whole Map (#401) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4333 · Zuma Guardian (#78) / D1103 (#35) / Whole Map (#401)

| 字段 | 值 |
|---|---|
| Monster | Zuma Guardian (#78) |
| Region | D1103 (#35) / Whole Map (#401) |
| EventSpawn | false |
| Delay | 1 |
| Count | 100 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4334 · Vicious Rat (#79) / D1103 (#35) / Respawn Area 1 (#406)

| 字段 | 值 |
|---|---|
| Monster | Vicious Rat (#79) |
| Region | D1103 (#35) / Respawn Area 1 (#406) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4335 · Zuma Sharpshooter (#76) / D1103 (#35) / Respawn Area 1 (#406)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1103 (#35) / Respawn Area 1 (#406) |
| EventSpawn | false |
| Delay | 15 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4336 · Zuma Fanatic (#77) / D1103 (#35) / Respawn Area 1 (#406)

| 字段 | 值 |
|---|---|
| Monster | Zuma Fanatic (#77) |
| Region | D1103 (#35) / Respawn Area 1 (#406) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4337 · Zuma Guardian (#78) / D1103 (#35) / Respawn Area 1 (#406)

| 字段 | 值 |
|---|---|
| Monster | Zuma Guardian (#78) |
| Region | D1103 (#35) / Respawn Area 1 (#406) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4338 · Vicious Rat (#79) / D1104 (#36) / Whole Map (#407)

| 字段 | 值 |
|---|---|
| Monster | Vicious Rat (#79) |
| Region | D1104 (#36) / Whole Map (#407) |
| EventSpawn | false |
| Delay | 1 |
| Count | 140 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4339 · Zuma Sharpshooter (#76) / D1104 (#36) / Whole Map (#407)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1104 (#36) / Whole Map (#407) |
| EventSpawn | false |
| Delay | 1 |
| Count | 360 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4340 · Zuma Fanatic (#77) / D1104 (#36) / Whole Map (#407)

| 字段 | 值 |
|---|---|
| Monster | Zuma Fanatic (#77) |
| Region | D1104 (#36) / Whole Map (#407) |
| EventSpawn | false |
| Delay | 1 |
| Count | 400 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4341 · Zuma Guardian (#78) / D1104 (#36) / Whole Map (#407)

| 字段 | 值 |
|---|---|
| Monster | Zuma Guardian (#78) |
| Region | D1104 (#36) / Whole Map (#407) |
| EventSpawn | false |
| Delay | 1 |
| Count | 400 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4342 · Vicious Rat (#79) / D1104 (#36) / Respawn Area 1 (#412)

| 字段 | 值 |
|---|---|
| Monster | Vicious Rat (#79) |
| Region | D1104 (#36) / Respawn Area 1 (#412) |
| EventSpawn | false |
| Delay | 15 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4343 · Zuma Sharpshooter (#76) / D1104 (#36) / Respawn Area 1 (#412)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1104 (#36) / Respawn Area 1 (#412) |
| EventSpawn | false |
| Delay | 15 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4344 · Zuma Fanatic (#77) / D1104 (#36) / Respawn Area 1 (#412)

| 字段 | 值 |
|---|---|
| Monster | Zuma Fanatic (#77) |
| Region | D1104 (#36) / Respawn Area 1 (#412) |
| EventSpawn | false |
| Delay | 15 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4345 · Zuma Guardian (#78) / D1104 (#36) / Respawn Area 1 (#412)

| 字段 | 值 |
|---|---|
| Monster | Zuma Guardian (#78) |
| Region | D1104 (#36) / Respawn Area 1 (#412) |
| EventSpawn | false |
| Delay | 15 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4347 · Zuma Sharpshooter (#76) / D1105 (#37) / Whole Map (#413)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1105 (#37) / Whole Map (#413) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4348 · Zuma Fanatic (#77) / D1105 (#37) / Whole Map (#413)

| 字段 | 值 |
|---|---|
| Monster | Zuma Fanatic (#77) |
| Region | D1105 (#37) / Whole Map (#413) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4349 · Zuma Guardian (#78) / D1105 (#37) / Whole Map (#413)

| 字段 | 值 |
|---|---|
| Monster | Zuma Guardian (#78) |
| Region | D1105 (#37) / Whole Map (#413) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4350 · Zuma Keeper (#80) / D1105 (#37) / Whole Map (#413)

| 字段 | 值 |
|---|---|
| Monster | Zuma Keeper (#80) |
| Region | D1105 (#37) / Whole Map (#413) |
| EventSpawn | false |
| Delay | 30 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4352 · Zuma Sharpshooter (#76) / D1105 (#37) / Respawn Area 1 (#418)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1105 (#37) / Respawn Area 1 (#418) |
| EventSpawn | false |
| Delay | 15 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4353 · Zuma Fanatic (#77) / D1105 (#37) / Respawn Area 1 (#418)

| 字段 | 值 |
|---|---|
| Monster | Zuma Fanatic (#77) |
| Region | D1105 (#37) / Respawn Area 1 (#418) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4354 · Zuma Guardian (#78) / D1105 (#37) / Respawn Area 1 (#418)

| 字段 | 值 |
|---|---|
| Monster | Zuma Guardian (#78) |
| Region | D1105 (#37) / Respawn Area 1 (#418) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4355 · Zuma Sharpshooter (#76) / D1105 (#37) / Respawn Area 2 (#419)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1105 (#37) / Respawn Area 2 (#419) |
| EventSpawn | false |
| Delay | 15 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4356 · Zuma Fanatic (#77) / D1105 (#37) / Respawn Area 2 (#419)

| 字段 | 值 |
|---|---|
| Monster | Zuma Fanatic (#77) |
| Region | D1105 (#37) / Respawn Area 2 (#419) |
| EventSpawn | false |
| Delay | 15 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4357 · Zuma Guardian (#78) / D1105 (#37) / Respawn Area 2 (#419)

| 字段 | 值 |
|---|---|
| Monster | Zuma Guardian (#78) |
| Region | D1105 (#37) / Respawn Area 2 (#419) |
| EventSpawn | false |
| Delay | 15 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4358 · Zuma Sharpshooter (#76) / D1105 (#37) / Respawn Area 3 (#420)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1105 (#37) / Respawn Area 3 (#420) |
| EventSpawn | false |
| Delay | 15 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4359 · Zuma Fanatic (#77) / D1105 (#37) / Respawn Area 3 (#420)

| 字段 | 值 |
|---|---|
| Monster | Zuma Fanatic (#77) |
| Region | D1105 (#37) / Respawn Area 3 (#420) |
| EventSpawn | false |
| Delay | 15 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4360 · Zuma Guardian (#78) / D1105 (#37) / Respawn Area 3 (#420)

| 字段 | 值 |
|---|---|
| Monster | Zuma Guardian (#78) |
| Region | D1105 (#37) / Respawn Area 3 (#420) |
| EventSpawn | false |
| Delay | 15 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4361 · Zuma Sharpshooter (#76) / D1105 (#37) / Respawn Area 4 (#421)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1105 (#37) / Respawn Area 4 (#421) |
| EventSpawn | false |
| Delay | 15 |
| Count | 25 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4362 · Zuma Fanatic (#77) / D1105 (#37) / Respawn Area 4 (#421)

| 字段 | 值 |
|---|---|
| Monster | Zuma Fanatic (#77) |
| Region | D1105 (#37) / Respawn Area 4 (#421) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4363 · Zuma Guardian (#78) / D1105 (#37) / Respawn Area 4 (#421)

| 字段 | 值 |
|---|---|
| Monster | Zuma Guardian (#78) |
| Region | D1105 (#37) / Respawn Area 4 (#421) |
| EventSpawn | false |
| Delay | 15 |
| Count | 50 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4364 · Zuma Sharpshooter (#76) / D1105 (#37) / Respawn Area 5 (#422)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1105 (#37) / Respawn Area 5 (#422) |
| EventSpawn | false |
| Delay | 15 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4365 · Zuma Fanatic (#77) / D1105 (#37) / Respawn Area 5 (#422)

| 字段 | 值 |
|---|---|
| Monster | Zuma Fanatic (#77) |
| Region | D1105 (#37) / Respawn Area 5 (#422) |
| EventSpawn | false |
| Delay | 15 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4366 · Zuma Guardian (#78) / D1105 (#37) / Respawn Area 5 (#422)

| 字段 | 值 |
|---|---|
| Monster | Zuma Guardian (#78) |
| Region | D1105 (#37) / Respawn Area 5 (#422) |
| EventSpawn | false |
| Delay | 15 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4367 · Zuma King (#81) / D1106 (#38) / Zumataurus (#426)

| 字段 | 值 |
|---|---|
| Monster | Zuma King (#81) |
| Region | D1106 (#38) / Zumataurus (#426) |
| EventSpawn | true |
| Delay | 300 |
| Count | 1 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4368 · Zuma Sharpshooter (#76) / D1106 (#38) / Whole Map (#423)

| 字段 | 值 |
|---|---|
| Monster | Zuma Sharpshooter (#76) |
| Region | D1106 (#38) / Whole Map (#423) |
| EventSpawn | false |
| Delay | 5 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4369 · Zuma Fanatic (#77) / D1106 (#38) / Whole Map (#423)

| 字段 | 值 |
|---|---|
| Monster | Zuma Fanatic (#77) |
| Region | D1106 (#38) / Whole Map (#423) |
| EventSpawn | false |
| Delay | 5 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4370 · Zuma Guardian (#78) / D1106 (#38) / Whole Map (#423)

| 字段 | 值 |
|---|---|
| Monster | Zuma Guardian (#78) |
| Region | D1106 (#38) / Whole Map (#423) |
| EventSpawn | false |
| Delay | 5 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4371 · Minotaur (#107) / D1001 (#16) / Whole Map (#258)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D1001 (#16) / Whole Map (#258) |
| EventSpawn | false |
| Delay | 1 |
| Count | 250 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4372 · Frost Minotaur (#108) / D1001 (#16) / Whole Map (#258)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D1001 (#16) / Whole Map (#258) |
| EventSpawn | false |
| Delay | 1 |
| Count | 66 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4373 · Shock Minotaur (#110) / D1001 (#16) / Whole Map (#258)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D1001 (#16) / Whole Map (#258) |
| EventSpawn | false |
| Delay | 1 |
| Count | 66 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4374 · Fury Minotaur (#112) / D1001 (#16) / Whole Map (#258)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D1001 (#16) / Whole Map (#258) |
| EventSpawn | false |
| Delay | 1 |
| Count | 66 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4375 · Flame Minotaur (#113) / D1001 (#16) / Whole Map (#258)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D1001 (#16) / Whole Map (#258) |
| EventSpawn | false |
| Delay | 1 |
| Count | 66 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4377 · Minotaur (#107) / D1001 (#16) / Respawn Area 1 (#259)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D1001 (#16) / Respawn Area 1 (#259) |
| EventSpawn | false |
| Delay | 10 |
| Count | 4 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4378 · Frost Minotaur (#108) / D1001 (#16) / Respawn Area 1 (#259)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D1001 (#16) / Respawn Area 1 (#259) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4379 · Shock Minotaur (#110) / D1001 (#16) / Respawn Area 1 (#259)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D1001 (#16) / Respawn Area 1 (#259) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4380 · Fury Minotaur (#112) / D1001 (#16) / Respawn Area 1 (#259)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D1001 (#16) / Respawn Area 1 (#259) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4381 · Flame Minotaur (#113) / D1001 (#16) / Respawn Area 1 (#259)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D1001 (#16) / Respawn Area 1 (#259) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4382 · Minotaur (#107) / D1001 (#16) / Respawn Area 2 (#260)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D1001 (#16) / Respawn Area 2 (#260) |
| EventSpawn | false |
| Delay | 10 |
| Count | 4 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4383 · Frost Minotaur (#108) / D1001 (#16) / Respawn Area 2 (#260)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D1001 (#16) / Respawn Area 2 (#260) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4384 · Shock Minotaur (#110) / D1001 (#16) / Respawn Area 2 (#260)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D1001 (#16) / Respawn Area 2 (#260) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4385 · Fury Minotaur (#112) / D1001 (#16) / Respawn Area 2 (#260)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D1001 (#16) / Respawn Area 2 (#260) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4386 · Flame Minotaur (#113) / D1001 (#16) / Respawn Area 2 (#260)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D1001 (#16) / Respawn Area 2 (#260) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4387 · Minotaur (#107) / D1001 (#16) / Respawn Area 3 (#261)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D1001 (#16) / Respawn Area 3 (#261) |
| EventSpawn | false |
| Delay | 10 |
| Count | 4 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4388 · Frost Minotaur (#108) / D1001 (#16) / Respawn Area 3 (#261)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D1001 (#16) / Respawn Area 3 (#261) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4389 · Shock Minotaur (#110) / D1001 (#16) / Respawn Area 3 (#261)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D1001 (#16) / Respawn Area 3 (#261) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4390 · Fury Minotaur (#112) / D1001 (#16) / Respawn Area 3 (#261)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D1001 (#16) / Respawn Area 3 (#261) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4391 · Flame Minotaur (#113) / D1001 (#16) / Respawn Area 3 (#261)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D1001 (#16) / Respawn Area 3 (#261) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4392 · Minotaur (#107) / D1002 (#17) / Whole Map (#268)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D1002 (#17) / Whole Map (#268) |
| EventSpawn | false |
| Delay | 1 |
| Count | 222 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4393 · Frost Minotaur (#108) / D1002 (#17) / Whole Map (#268)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D1002 (#17) / Whole Map (#268) |
| EventSpawn | false |
| Delay | 1 |
| Count | 222 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4394 · Shock Minotaur (#110) / D1002 (#17) / Whole Map (#268)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D1002 (#17) / Whole Map (#268) |
| EventSpawn | false |
| Delay | 1 |
| Count | 222 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4395 · Fury Minotaur (#112) / D1002 (#17) / Whole Map (#268)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D1002 (#17) / Whole Map (#268) |
| EventSpawn | false |
| Delay | 1 |
| Count | 222 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4396 · Flame Minotaur (#113) / D1002 (#17) / Whole Map (#268)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D1002 (#17) / Whole Map (#268) |
| EventSpawn | false |
| Delay | 1 |
| Count | 222 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4399 · Shock Minotaur (#110) / D1002 (#17) / Respawn Area 1 (#269)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D1002 (#17) / Respawn Area 1 (#269) |
| EventSpawn | false |
| Delay | 10 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4406 · Flame Minotaur (#113) / D1002 (#17) / Respawn Area 2 (#270)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D1002 (#17) / Respawn Area 2 (#270) |
| EventSpawn | false |
| Delay | 10 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4411 · Frost Minotaur (#108) / D1002 (#17) / Respawn Area 3 (#271)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D1002 (#17) / Respawn Area 3 (#271) |
| EventSpawn | false |
| Delay | 10 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4416 · Fury Minotaur (#112) / D1002 (#17) / Respawn Area 4 (#272)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D1002 (#17) / Respawn Area 4 (#272) |
| EventSpawn | false |
| Delay | 10 |
| Count | 30 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4418 · Minotaur (#107) / D10031 (#18) / Whole Map (#279)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D10031 (#18) / Whole Map (#279) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4419 · Frost Minotaur (#108) / D10031 (#18) / Whole Map (#279)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D10031 (#18) / Whole Map (#279) |
| EventSpawn | false |
| Delay | 1 |
| Count | 200 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4420 · Shock Minotaur (#110) / D10031 (#18) / Whole Map (#279)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D10031 (#18) / Whole Map (#279) |
| EventSpawn | false |
| Delay | 1 |
| Count | 266 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4421 · Fury Minotaur (#112) / D10031 (#18) / Whole Map (#279)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D10031 (#18) / Whole Map (#279) |
| EventSpawn | false |
| Delay | 1 |
| Count | 266 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4422 · Flame Minotaur (#113) / D10031 (#18) / Whole Map (#279)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D10031 (#18) / Whole Map (#279) |
| EventSpawn | false |
| Delay | 1 |
| Count | 266 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4423 · Minotaur (#107) / D10031 (#18) / Respawn Area 1 (#280)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D10031 (#18) / Respawn Area 1 (#280) |
| EventSpawn | false |
| Delay | 10 |
| Count | 4 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4424 · Frost Minotaur (#108) / D10031 (#18) / Respawn Area 1 (#280)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D10031 (#18) / Respawn Area 1 (#280) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4425 · Shock Minotaur (#110) / D10031 (#18) / Respawn Area 1 (#280)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D10031 (#18) / Respawn Area 1 (#280) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4426 · Fury Minotaur (#112) / D10031 (#18) / Respawn Area 1 (#280)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D10031 (#18) / Respawn Area 1 (#280) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4427 · Flame Minotaur (#113) / D10031 (#18) / Respawn Area 1 (#280)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D10031 (#18) / Respawn Area 1 (#280) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4428 · Minotaur (#107) / D10031 (#18) / Respawn Area 2 (#281)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D10031 (#18) / Respawn Area 2 (#281) |
| EventSpawn | false |
| Delay | 10 |
| Count | 4 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4429 · Frost Minotaur (#108) / D10031 (#18) / Respawn Area 2 (#281)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D10031 (#18) / Respawn Area 2 (#281) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4430 · Shock Minotaur (#110) / D10031 (#18) / Respawn Area 2 (#281)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D10031 (#18) / Respawn Area 2 (#281) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4431 · Fury Minotaur (#112) / D10031 (#18) / Respawn Area 2 (#281)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D10031 (#18) / Respawn Area 2 (#281) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4432 · Flame Minotaur (#113) / D10031 (#18) / Respawn Area 2 (#281)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D10031 (#18) / Respawn Area 2 (#281) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4433 · Minotaur (#107) / D10031 (#18) / Respawn Area 3 (#282)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D10031 (#18) / Respawn Area 3 (#282) |
| EventSpawn | false |
| Delay | 10 |
| Count | 4 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4434 · Frost Minotaur (#108) / D10031 (#18) / Respawn Area 3 (#282)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D10031 (#18) / Respawn Area 3 (#282) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4435 · Shock Minotaur (#110) / D10031 (#18) / Respawn Area 3 (#282)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D10031 (#18) / Respawn Area 3 (#282) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4436 · Fury Minotaur (#112) / D10031 (#18) / Respawn Area 3 (#282)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D10031 (#18) / Respawn Area 3 (#282) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4437 · Flame Minotaur (#113) / D10031 (#18) / Respawn Area 3 (#282)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D10031 (#18) / Respawn Area 3 (#282) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4438 · Minotaur (#107) / D10031 (#18) / Respawn Area 4 (#283)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D10031 (#18) / Respawn Area 4 (#283) |
| EventSpawn | false |
| Delay | 10 |
| Count | 4 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4439 · Frost Minotaur (#108) / D10031 (#18) / Respawn Area 4 (#283)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D10031 (#18) / Respawn Area 4 (#283) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 2000 |
| RespawnIndex | 0 |

### #4440 · Shock Minotaur (#110) / D10031 (#18) / Respawn Area 4 (#283)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D10031 (#18) / Respawn Area 4 (#283) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4441 · Fury Minotaur (#112) / D10031 (#18) / Respawn Area 4 (#283)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D10031 (#18) / Respawn Area 4 (#283) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4442 · Flame Minotaur (#113) / D10031 (#18) / Respawn Area 4 (#283)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D10031 (#18) / Respawn Area 4 (#283) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4443 · Minotaur (#107) / D10031 (#18) / Respawn Area 5 (#284)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D10031 (#18) / Respawn Area 5 (#284) |
| EventSpawn | false |
| Delay | 10 |
| Count | 4 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4444 · Frost Minotaur (#108) / D10031 (#18) / Respawn Area 5 (#284)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D10031 (#18) / Respawn Area 5 (#284) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4445 · Shock Minotaur (#110) / D10031 (#18) / Respawn Area 5 (#284)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D10031 (#18) / Respawn Area 5 (#284) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4446 · Fury Minotaur (#112) / D10031 (#18) / Respawn Area 5 (#284)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D10031 (#18) / Respawn Area 5 (#284) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4447 · Flame Minotaur (#113) / D10031 (#18) / Respawn Area 5 (#284)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D10031 (#18) / Respawn Area 5 (#284) |
| EventSpawn | false |
| Delay | 10 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4448 · Minotaur (#107) / D10031 (#18) / Respawn Area 6 (#285)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D10031 (#18) / Respawn Area 6 (#285) |
| EventSpawn | false |
| Delay | 10 |
| Count | 8 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4449 · Frost Minotaur (#108) / D10031 (#18) / Respawn Area 6 (#285)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D10031 (#18) / Respawn Area 6 (#285) |
| EventSpawn | false |
| Delay | 10 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4450 · Shock Minotaur (#110) / D10031 (#18) / Respawn Area 6 (#285)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D10031 (#18) / Respawn Area 6 (#285) |
| EventSpawn | false |
| Delay | 10 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4451 · Fury Minotaur (#112) / D10031 (#18) / Respawn Area 6 (#285)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D10031 (#18) / Respawn Area 6 (#285) |
| EventSpawn | false |
| Delay | 10 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4452 · Flame Minotaur (#113) / D10031 (#18) / Respawn Area 6 (#285)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D10031 (#18) / Respawn Area 6 (#285) |
| EventSpawn | false |
| Delay | 10 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4453 · Minotaur (#107) / D10031 (#18) / Respawn Area 7 (#288)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D10031 (#18) / Respawn Area 7 (#288) |
| EventSpawn | false |
| Delay | 10 |
| Count | 2 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4454 · Frost Minotaur (#108) / D10031 (#18) / Respawn Area 7 (#288)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D10031 (#18) / Respawn Area 7 (#288) |
| EventSpawn | false |
| Delay | 10 |
| Count | 3 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4455 · Shock Minotaur (#110) / D10031 (#18) / Respawn Area 7 (#288)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D10031 (#18) / Respawn Area 7 (#288) |
| EventSpawn | false |
| Delay | 10 |
| Count | 3 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4456 · Fury Minotaur (#112) / D10031 (#18) / Respawn Area 7 (#288)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D10031 (#18) / Respawn Area 7 (#288) |
| EventSpawn | false |
| Delay | 10 |
| Count | 3 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4457 · Flame Minotaur (#113) / D10031 (#18) / Respawn Area 7 (#288)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D10031 (#18) / Respawn Area 7 (#288) |
| EventSpawn | false |
| Delay | 10 |
| Count | 3 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4458 · Banya Left Guard (#111) / D10031 (#18) / Whole Map (#279)

| 字段 | 值 |
|---|---|
| Monster | Banya Left Guard (#111) |
| Region | D10031 (#18) / Whole Map (#279) |
| EventSpawn | false |
| Delay | 1 |
| Count | 266 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4459 · Banya Right Guard (#109) / D10031 (#18) / Whole Map (#279)

| 字段 | 值 |
|---|---|
| Monster | Banya Right Guard (#109) |
| Region | D10031 (#18) / Whole Map (#279) |
| EventSpawn | false |
| Delay | 1 |
| Count | 155 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4460 · Minotaur (#107) / D10032 (#19) / Whole Map (#289)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D10032 (#19) / Whole Map (#289) |
| EventSpawn | false |
| Delay | 10 |
| Count | 10 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4461 · Frost Minotaur (#108) / D10032 (#19) / Whole Map (#289)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D10032 (#19) / Whole Map (#289) |
| EventSpawn | false |
| Delay | 10 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4462 · Shock Minotaur (#110) / D10032 (#19) / Whole Map (#289)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D10032 (#19) / Whole Map (#289) |
| EventSpawn | false |
| Delay | 10 |
| Count | 25 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4463 · Fury Minotaur (#112) / D10032 (#19) / Whole Map (#289)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D10032 (#19) / Whole Map (#289) |
| EventSpawn | false |
| Delay | 10 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4464 · Flame Minotaur (#113) / D10032 (#19) / Whole Map (#289)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D10032 (#19) / Whole Map (#289) |
| EventSpawn | false |
| Delay | 10 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4465 · Banya Left Guard (#111) / D10032 (#19) / Whole Map (#289)

| 字段 | 值 |
|---|---|
| Monster | Banya Left Guard (#111) |
| Region | D10032 (#19) / Whole Map (#289) |
| EventSpawn | false |
| Delay | 10 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4466 · Banya Right Guard (#109) / D10032 (#19) / Whole Map (#289)

| 字段 | 值 |
|---|---|
| Monster | Banya Right Guard (#109) |
| Region | D10032 (#19) / Whole Map (#289) |
| EventSpawn | false |
| Delay | 10 |
| Count | 15 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4467 · Minotaur (#107) / D1004 (#20) / Whole Map (#294)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D1004 (#20) / Whole Map (#294) |
| EventSpawn | false |
| Delay | 5 |
| Count | 20 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4468 · Frost Minotaur (#108) / D1004 (#20) / Whole Map (#294)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D1004 (#20) / Whole Map (#294) |
| EventSpawn | false |
| Delay | 5 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4469 · Shock Minotaur (#110) / D1004 (#20) / Whole Map (#294)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D1004 (#20) / Whole Map (#294) |
| EventSpawn | false |
| Delay | 5 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4470 · Fury Minotaur (#112) / D1004 (#20) / Whole Map (#294)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D1004 (#20) / Whole Map (#294) |
| EventSpawn | false |
| Delay | 5 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4471 · Flame Minotaur (#113) / D1004 (#20) / Whole Map (#294)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D1004 (#20) / Whole Map (#294) |
| EventSpawn | false |
| Delay | 5 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4472 · Banya Left Guard (#111) / D1004 (#20) / Whole Map (#294)

| 字段 | 值 |
|---|---|
| Monster | Banya Left Guard (#111) |
| Region | D1004 (#20) / Whole Map (#294) |
| EventSpawn | false |
| Delay | 5 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4473 · Banya Right Guard (#109) / D1004 (#20) / Whole Map (#294)

| 字段 | 值 |
|---|---|
| Monster | Banya Right Guard (#109) |
| Region | D1004 (#20) / Whole Map (#294) |
| EventSpawn | false |
| Delay | 5 |
| Count | 40 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4474 · Minotaur (#107) / D1004 (#20) / Respawn Area 1 (#299)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D1004 (#20) / Respawn Area 1 (#299) |
| EventSpawn | false |
| Delay | 15 |
| Count | 5 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4475 · Frost Minotaur (#108) / D1004 (#20) / Respawn Area 1 (#299)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D1004 (#20) / Respawn Area 1 (#299) |
| EventSpawn | false |
| Delay | 15 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4476 · Shock Minotaur (#110) / D1004 (#20) / Respawn Area 1 (#299)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D1004 (#20) / Respawn Area 1 (#299) |
| EventSpawn | false |
| Delay | 15 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4477 · Fury Minotaur (#112) / D1004 (#20) / Respawn Area 1 (#299)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D1004 (#20) / Respawn Area 1 (#299) |
| EventSpawn | false |
| Delay | 15 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4478 · Flame Minotaur (#113) / D1004 (#20) / Respawn Area 1 (#299)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D1004 (#20) / Respawn Area 1 (#299) |
| EventSpawn | false |
| Delay | 15 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4479 · Banya Left Guard (#111) / D1004 (#20) / Respawn Area 1 (#299)

| 字段 | 值 |
|---|---|
| Monster | Banya Left Guard (#111) |
| Region | D1004 (#20) / Respawn Area 1 (#299) |
| EventSpawn | false |
| Delay | 15 |
| Count | 4 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4480 · Banya Right Guard (#109) / D1004 (#20) / Respawn Area 1 (#299)

| 字段 | 值 |
|---|---|
| Monster | Banya Right Guard (#109) |
| Region | D1004 (#20) / Respawn Area 1 (#299) |
| EventSpawn | false |
| Delay | 15 |
| Count | 4 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4481 · Minotaur (#107) / D1004 (#20) / Respawn Area 2 (#300)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D1004 (#20) / Respawn Area 2 (#300) |
| EventSpawn | false |
| Delay | 15 |
| Count | 5 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4482 · Frost Minotaur (#108) / D1004 (#20) / Respawn Area 2 (#300)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D1004 (#20) / Respawn Area 2 (#300) |
| EventSpawn | false |
| Delay | 15 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4483 · Shock Minotaur (#110) / D1004 (#20) / Respawn Area 2 (#300)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D1004 (#20) / Respawn Area 2 (#300) |
| EventSpawn | false |
| Delay | 15 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4484 · Fury Minotaur (#112) / D1004 (#20) / Respawn Area 2 (#300)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D1004 (#20) / Respawn Area 2 (#300) |
| EventSpawn | false |
| Delay | 15 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4485 · Flame Minotaur (#113) / D1004 (#20) / Respawn Area 2 (#300)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D1004 (#20) / Respawn Area 2 (#300) |
| EventSpawn | false |
| Delay | 15 |
| Count | 7 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4486 · Banya Left Guard (#111) / D1004 (#20) / Respawn Area 2 (#300)

| 字段 | 值 |
|---|---|
| Monster | Banya Left Guard (#111) |
| Region | D1004 (#20) / Respawn Area 2 (#300) |
| EventSpawn | false |
| Delay | 15 |
| Count | 4 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4487 · Banya Right Guard (#109) / D1004 (#20) / Respawn Area 2 (#300)

| 字段 | 值 |
|---|---|
| Monster | Banya Right Guard (#109) |
| Region | D1004 (#20) / Respawn Area 2 (#300) |
| EventSpawn | false |
| Delay | 15 |
| Count | 4 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4488 · Minotaur (#107) / D1005 (#21) / Whole Map (#301)

| 字段 | 值 |
|---|---|
| Monster | Minotaur (#107) |
| Region | D1005 (#21) / Whole Map (#301) |
| EventSpawn | false |
| Delay | 1 |
| Count | 55 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4489 · Frost Minotaur (#108) / D1005 (#21) / Whole Map (#301)

| 字段 | 值 |
|---|---|
| Monster | Frost Minotaur (#108) |
| Region | D1005 (#21) / Whole Map (#301) |
| EventSpawn | false |
| Delay | 1 |
| Count | 55 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4490 · Shock Minotaur (#110) / D1005 (#21) / Whole Map (#301)

| 字段 | 值 |
|---|---|
| Monster | Shock Minotaur (#110) |
| Region | D1005 (#21) / Whole Map (#301) |
| EventSpawn | false |
| Delay | 1 |
| Count | 77 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4491 · Fury Minotaur (#112) / D1005 (#21) / Whole Map (#301)

| 字段 | 值 |
|---|---|
| Monster | Fury Minotaur (#112) |
| Region | D1005 (#21) / Whole Map (#301) |
| EventSpawn | false |
| Delay | 1 |
| Count | 77 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4492 · Flame Minotaur (#113) / D1005 (#21) / Whole Map (#301)

| 字段 | 值 |
|---|---|
| Monster | Flame Minotaur (#113) |
| Region | D1005 (#21) / Whole Map (#301) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

### #4493 · Banya Left Guard (#111) / D1005 (#21) / Whole Map (#301)

| 字段 | 值 |
|---|---|
| Monster | Banya Left Guard (#111) |
| Region | D1005 (#21) / Whole Map (#301) |
| EventSpawn | false |
| Delay | 1 |
| Count | 60 |
| DropSet | 0 |
| Announce | false |
| EasterEventChance | 50 |
| RespawnIndex | 0 |

