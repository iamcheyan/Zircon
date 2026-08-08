<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改。重新生成: dotnet run --project Tools/SystemDbProbe -- --dump docs/database -->

# NPC 页面（NPCPage）

> 记录 #1 – #336，共 302 条（第 1/2 部分）。

[README](../README.md) · [下一部分 →](NPCPage.2.md)

### #1 · Basic Potion Main

| 字段 | 值 |
|---|---|
| Description | Basic Potion Main |
| DialogType | None |
| Say | Drugs, Get your Drugs here,<br><br>You there, You look like an addict, Wanna by some potions!?<br><br>[Browse Potions:1]<br><br>[Back away slowly:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #2 · Basic Potion BuySell

| 字段 | 值 |
|---|---|
| Description | Basic Potion BuySell |
| DialogType | BuySell |
| Say | Excellent...<br><br>Now spend every penny.<br><br><br><br>[Back:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 47 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 1 条（明细见 [NPCType.md](NPCType.md)） |

### #3 · Meat Main

| 字段 | 值 |
|---|---|
| Description | Meat Main |
| DialogType | None |
| Say | Welcome to my store,<br><br>I have been in business for years, I know all of the best cuts.<br><br>What can I do for you?<br><br>[Browse Store:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #4 · Meat BuySell

| 字段 | 值 |
|---|---|
| Description | Meat BuySell |
| DialogType | BuySell |
| Say | Be with you in a moment,<br><br>Take a look let me know if there is anything you would like...<br><br>I am getting low on stock so I will pay well for some high quality meat.<br><br><br>[Back:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 5 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 1 条（明细见 [NPCType.md](NPCType.md)） |

### #5 · Weapon Main

| 字段 | 值 |
|---|---|
| Description | Weapon Main |
| DialogType | None |
| Say | What can I do for you, <br><br>My services are:<br><br>[Browse Weapons:1]<br>[Repair Weapons:2]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #6 · Weapon BuySell

| 字段 | 值 |
|---|---|
| Description | Weapon BuySell |
| DialogType | BuySell |
| Say | Take your time,<br><br>I've got all day.<br><br><br>[Back:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 20 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 2 条（明细见 [NPCType.md](NPCType.md)） |

### #7 · Weapon Repair

| 字段 | 值 |
|---|---|
| Description | Weapon Repair |
| DialogType | Repair |
| Say | I can do a cheap repair or I can do a perfect repair, the choice is yours.<br><br><br>[Back:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |
| Types | `NPCType` × 2 条（明细见 [NPCType.md](NPCType.md)） |

### #8 · Jewellery Main

| 字段 | 值 |
|---|---|
| Description | Jewellery Main |
| DialogType | None |
| Say | One moment,<br> <br>Just putting this shipment away.<br><br>[Browse Rings:1]<br>[Browse Bracelets:2]<br>[Browse Necklaces:3]<br><br>[Repair Jewellery:4]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 4 条（明细见 [NPCButton.md](NPCButton.md)） |

### #9 · Ring BuySell

| 字段 | 值 |
|---|---|
| Description | Ring BuySell |
| DialogType | BuySell |
| Say | I have some new rings for sale,<br><br>Take your time, they are all genuine, none of them are counterfeits...<br><br>[Browse Bracelets:1]<br><br>[Browse Necklaces:2]<br><br>[Back:3]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 12 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 3 条（明细见 [NPCType.md](NPCType.md)） |

### #10 · Bracelet BuySell

| 字段 | 值 |
|---|---|
| Description | Bracelet BuySell |
| DialogType | BuySell |
| Say | I have some new bracelets for sale,<br><br>Take your time, they are all genuine, none of them are counterfeits...<br><br>[Browse Rings:1]<br><br>[Browse Necklaces:2]<br><br>[Back:3]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 11 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 3 条（明细见 [NPCType.md](NPCType.md)） |

### #11 · Necklace BuySell

| 字段 | 值 |
|---|---|
| Description | Necklace BuySell |
| DialogType | BuySell |
| Say | I have some new necklaces for sale,<br><br>Take your time, they are all genuine, none of them are counterfeits...<br><br>[Browse Rings:1]<br><br>[Browse Bracelets:2]<br><br>[Back:3]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 11 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 3 条（明细见 [NPCType.md](NPCType.md)） |

### #12 · Jewellery Repair

| 字段 | 值 |
|---|---|
| Description | Jewellery Repair |
| DialogType | Repair |
| Say | Lay your precious "valuables" on this table,<br><br>I shall inspect them thoroughly...<br><br>[Back:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |
| Types | `NPCType` × 3 条（明细见 [NPCType.md](NPCType.md)） |

### #13 · Book Main

| 字段 | 值 |
|---|---|
| Description | Book Main |
| DialogType | None |
| Say | Quiet, I am reading my books, take a look and then tell me what you want.<br><br>[Warrior Books:1]<br>[Wizard Books:2]<br>[Taoist Books:3]<br>[Assassin Books:4]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 4 条（明细见 [NPCButton.md](NPCButton.md)） |

### #14 · Warrior Books

| 字段 | 值 |
|---|---|
| Description | Warrior Books |
| DialogType | BuySell |
| Say | Quiet, I am reading my books, take a look and then tell me what you want.<br><br>Warrior Books<br>[Wizard Books:2]<br>[Taoist Books:3]<br>[Assassin Books:4]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 4 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 1 条（明细见 [NPCType.md](NPCType.md)） |

### #15 · Wizard Books

| 字段 | 值 |
|---|---|
| Description | Wizard Books |
| DialogType | BuySell |
| Say | Quiet, I am reading my books, take a look and then tell me what you want.<br><br>[Warrior Books:1]<br>Wizard Books<br>[Taoist Books:3]<br>[Assassin Books:4]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 12 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 1 条（明细见 [NPCType.md](NPCType.md)） |

### #16 · Taoist Books

| 字段 | 值 |
|---|---|
| Description | Taoist Books |
| DialogType | BuySell |
| Say | Quiet, I am reading my books, take a look and then tell me what you want.<br><br>[Warrior Books:1]<br>[Wizard Books:2]<br>Taoist Books<br>[Assassin Books:4]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 9 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 1 条（明细见 [NPCType.md](NPCType.md)） |

### #17 · Assassin Books

| 字段 | 值 |
|---|---|
| Description | Assassin Books |
| DialogType | BuySell |
| Say | Quiet, I am reading my books, take a look and then tell me what you want.<br><br>[Warrior Books:1]<br>[Wizard Books:2]<br>[Taoist Books:3]<br>Assassin Books<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 6 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 1 条（明细见 [NPCType.md](NPCType.md)） |

### #18 · Essentials Main

| 字段 | 值 |
|---|---|
| Description | Essentials Main |
| DialogType | None |
| Say | I got what you need,<br><br>My prices are just,<br><br>[Browse Essentials:1]<br>[Browse Taoist Goods:2]<br>[Browse Dark Stones:3]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |

### #19 · Armour Main

| 字段 | 值 |
|---|---|
| Description | Armour Main |
| DialogType | None |
| Say | I've got some fabulous new outfits,<br><br>What are you interested in?<br><br>[Browse Armours:1]<br>[Browse Helmets and Shoes:2]<br>[Repair Armours:3]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |

### #20 · Collector BuySell

| 字段 | 值 |
|---|---|
| Description | Collector BuySell |
| DialogType | BuySell |
| Say | I'm a collector, <br><br>I collect items most consider worthless.<br><br><br>[Exit:0]<br><br> |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Types | `NPCType` × 1 条（明细见 [NPCType.md](NPCType.md)） |

### #21 · Essentials BuySell

| 字段 | 值 |
|---|---|
| Description | Essentials BuySell |
| DialogType | BuySell |
| Say | I should be getting some new stock soon!<br><br><br>[Browse Taoist Goods:2]<br>[Browse Dark Stones:3]<br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 7 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 5 条（明细见 [NPCType.md](NPCType.md)） |

### #22 · Armour BuySell

| 字段 | 值 |
|---|---|
| Description | Armour BuySell |
| DialogType | BuySell |
| Say | Armours and Clothing,<br><br>One size fits all.<br><br>[Back:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 16 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 3 条（明细见 [NPCType.md](NPCType.md)） |

### #23 · Armour Repair

| 字段 | 值 |
|---|---|
| Description | Armour Repair |
| DialogType | Repair |
| Say | Fine,<br><br>I will also fix the hobo suit.<br><br>[Back:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |
| Types | `NPCType` × 4 条（明细见 [NPCType.md](NPCType.md)） |

### #24 · Armour Other BuySell

| 字段 | 值 |
|---|---|
| Description | Armour Other BuySell |
| DialogType | BuySell |
| Say | Helmets and Shoes,<br><br>One size fits all.<br><br>[Back:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 6 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 3 条（明细见 [NPCType.md](NPCType.md)） |

### #25 · Well

| 字段 | 值 |
|---|---|
| Description | Well |
| DialogType | None |
| Say | — |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #30 · Teleport Fail Cost

| 字段 | 值 |
|---|---|
| Description | Teleport Fail Cost |
| DialogType | None |
| Say | Failed to teleport to destination,<br><br>Not enough gold.<br><br>[Exit:0] |
| Arguments | — |

### #31 · Teleport to Bichon Town

| 字段 | 值 |
|---|---|
| Description | Teleport to Bichon Town |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #32 · Teleport to Lost Paradise

| 字段 | 值 |
|---|---|
| Description | Teleport to Lost Paradise |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #33 · Teleport to Sabuk Keep

| 字段 | 值 |
|---|---|
| Description | Teleport to Sabuk Keep |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #34 · Teleport to BanyaVillage

| 字段 | 值 |
|---|---|
| Description | Teleport to BanyaVillage |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #35 · Teleport to MudWall

| 字段 | 值 |
|---|---|
| Description | Teleport to MudWall |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #36 · Teleport to Banya Island

| 字段 | 值 |
|---|---|
| Description | Teleport to Banya Island |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #37 · Teleport to Numa Village

| 字段 | 值 |
|---|---|
| Description | Teleport to Numa Village |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #38 · BT Teleporter

| 字段 | 值 |
|---|---|
| Description | BT Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Lost Paradise:1] 10,000 Gold<br>[Sabuk Keep:2] 3,000 Gold<br>[Banya Village:3] 5,000 Gold<br><br>[Bichon Castle:4] 10,000 Gold,  (Level 45+)<br><br><br>[Freedom Pass:5] [Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 5 条（明细见 [NPCButton.md](NPCButton.md)） |

### #39 · BV Teleporter

| 字段 | 值 |
|---|---|
| Description | BV Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Numa Village:1] 10,000 Gold<br>[Sabuk Keep:2] 3,000 Gold<br>[Bichon Town:3] 5,000 Gold<br><br>[Banya Island:4] 10,000 Gold (Level 25+)<br>[Infernal Island:5] 50,000 Gold<br><br>[Freedom Pass:6] [Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 6 条（明细见 [NPCButton.md](NPCButton.md)） |

### #40 · LP Teleporter

| 字段 | 值 |
|---|---|
| Description | LP Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Bichon Town:1] 10,000 Gold<br>[Desert Mud Wall:2] 3,000 Gold<br>[Numa Village:3] 5,000 Gold<br><br>[Taoist Temple:4] 10,000 Gold<br><br>[Frost Village:5] 20,000 Gold,  (Level 45+)<br><br>[Freedom Pass:6] [Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 6 条（明细见 [NPCButton.md](NPCButton.md)） |

### #41 · SK Teleporter

| 字段 | 值 |
|---|---|
| Description | SK Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Bichon Town:1] 3,000 Gold<br>[Desert Mud Wall:2] 5,000 Gold<br>[Banya Village:3] 3,000 Gold<br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |

### #42 · MW Teleporter

| 字段 | 值 |
|---|---|
| Description | MW Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Lost Paradise:1] 3,000 Gold<br>[Sabuk Keep:2] 5,000 Gold<br>[Numa Village:3] 3,000 Gold<br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |

### #43 · NV Teleporter

| 字段 | 值 |
|---|---|
| Description | NV Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Banya Village:1] 10,000 Gold<br>[Desert Mud Wall:2] 3,000 Gold<br>[Lost Paradise:3] 5,000 Gold<br>[Western Arids:5] 5,000,000 Gold (Level 60+)<br>[Arid Flats:6] 5,000,000 Gold + Freedom Pass (Level 60+)<br><br>[Freedom Pass:4] [Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 6 条（明细见 [NPCButton.md](NPCButton.md)） |

### #49 · BI Teleporter

| 字段 | 值 |
|---|---|
| Description | BI Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Banya Village:1] 10,000 Gold<br><br>[Lost Land:3] 30,000 Gold (Level 35+)<br><br><br>[Freedom Pass:2] [Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |

### #50 · BT 10K

| 字段 | 值 |
|---|---|
| Description | BT 10K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Bichon Town (#31) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #51 · BT 5K

| 字段 | 值 |
|---|---|
| Description | BT 5K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Bichon Town (#31) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #52 · BT 3K

| 字段 | 值 |
|---|---|
| Description | BT 3K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Bichon Town (#31) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #53 · SK 5K

| 字段 | 值 |
|---|---|
| Description | SK 5K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Sabuk Keep (#33) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #54 · SK 3K

| 字段 | 值 |
|---|---|
| Description | SK 3K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Sabuk Keep (#33) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #55 · MW 5K

| 字段 | 值 |
|---|---|
| Description | MW 5K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to MudWall (#35) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #56 · MW 3K

| 字段 | 值 |
|---|---|
| Description | MW 3K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to MudWall (#35) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #57 · BV 10K

| 字段 | 值 |
|---|---|
| Description | BV 10K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to BanyaVillage (#34) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #58 · BV 5K

| 字段 | 值 |
|---|---|
| Description | BV 5K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to BanyaVillage (#34) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #59 · BV 3K

| 字段 | 值 |
|---|---|
| Description | BV 3K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to BanyaVillage (#34) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #60 · NV 10K

| 字段 | 值 |
|---|---|
| Description | NV 10K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Numa Village (#37) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #61 · NV 5K

| 字段 | 值 |
|---|---|
| Description | NV 5K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Numa Village (#37) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #62 · NV 3K

| 字段 | 值 |
|---|---|
| Description | NV 3K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Numa Village (#37) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #63 · LP 10K

| 字段 | 值 |
|---|---|
| Description | LP 10K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Lost Paradise (#32) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #64 · LP 5K

| 字段 | 值 |
|---|---|
| Description | LP 5K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Lost Paradise (#32) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #65 · LP 3K

| 字段 | 值 |
|---|---|
| Description | LP 3K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Lost Paradise (#32) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #66 · BI 10K and Level 25+

| 字段 | 值 |
|---|---|
| Description | BI 10K and Level 25+ |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Banya Island (#36) |
| Arguments | — |
| Checks | `NPCCheck` × 2 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #68 · Amulet BuySell

| 字段 | 值 |
|---|---|
| Description | Amulet BuySell |
| DialogType | BuySell |
| Say | I should be getting some new stock soon!<br><br>[Browse Essentials:1]<br><br>[Browse Dark Stones:3]<br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 11 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 5 条（明细见 [NPCType.md](NPCType.md)） |

### #69 · DarkStone BuySell

| 字段 | 值 |
|---|---|
| Description | DarkStone BuySell |
| DialogType | BuySell |
| Say | I should be getting some new stock soon!<br><br>[Browse Essentials:1]<br>[Browse Taoist Goods:2]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 8 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 5 条（明细见 [NPCType.md](NPCType.md)） |

### #70 · Teleport Banya Hall

| 字段 | 值 |
|---|---|
| Description | Teleport Banya Hall |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #71 · Weapon Refiner

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner |
| DialogType | None |
| Say | I'm an artisan who refines weapons. Let me refine your weapon for you.<br><br>[Refine:1] your weapon. <br>[Retrieve:2] a refined weapon.<br>[Change:4] element of weapon at a cost of 100,000 Gold.<br>[Inquire:3] about refining.<br>[Inquire:5] about the refinement stone.<br>[Reset:6] Weapon Level<br>[Start:7] Master refining process.<br>[Start:8] Special refining process.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 8 条（明细见 [NPCButton.md](NPCButton.md)） |

### #72 · Weapon Refiner - Fail - No Weapon

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Fail - No Weapon |
| DialogType | None |
| Say | You are not holding a weapon.<br><br>Please come back when you have a weapon.<br><br><br>[Exit:0] |
| Arguments | — |

### #73 · Weapon Refiner - Change Element - Start

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Change Element - Start |
| DialogType | None |
| Say | What element would you like to embue your weapon with?<br><br>Change to [Fire:1] element.<br>Change to [Ice:2] element.<br>Change to [Lightning:3] element.<br>Change to [Wind:4] element.<br>Change to [Holy:5] element.<br>Change to [Dark:6] element.<br>Change to [Phantom:7] element.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 7 条（明细见 [NPCButton.md](NPCButton.md)） |

### #74 · Weapon Refiner - Change Element - Fail - No Elements

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Change Element - Fail - No Elements |
| DialogType | None |
| Say | The weapon you are holding does not contain any elemental properties.<br><br>I cannot help.<br><br><br>[Exit:0] |
| Arguments | — |

### #75 · Weapon Refiner - Change Element - Complete - Fire

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Change Element - Complete - Fire |
| DialogType | None |
| Say | Congratulations,<br><br>Your weapon is now Fire element.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 5 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #76 · Weapon Refiner - Fail - No Gold

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Fail - No Gold |
| DialogType | None |
| Say | You do not have enough Gold.<br><br>Please come back when you have enough Gold.<br><br><br>[Exit:0] |
| Arguments | — |

### #77 · Weapon Refiner - Change Element - Fail - Same Element

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Change Element - Fail - Same Element |
| DialogType | None |
| Say | Failed to change the element of your weapon...<br><br>Your weapon is already this element.<br><br>[Exit:0] |
| Arguments | — |

### #78 · Weapon Refiner - Change Element - Complete - Ice

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Change Element - Complete - Ice |
| DialogType | None |
| Say | Congratulations,<br><br>Your weapon is now Ice element.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 5 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #79 · Weapon Refiner - Change Element - Complete - Lightning

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Change Element - Complete - Lightning |
| DialogType | None |
| Say | Congratulations,<br><br>Your weapon is now Lightning element.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 5 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #80 · Weapon Refiner - Change Element - Complete - Wind

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Change Element - Complete - Wind |
| DialogType | None |
| Say | Congratulations,<br><br>Your weapon is now Wind element.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 5 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #81 · Weapon Refiner - Change Element - Complete - Holy

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Change Element - Complete - Holy |
| DialogType | None |
| Say | Congratulations,<br><br>Your weapon is now Holy element.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 5 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #82 · Weapon Refiner - Change Element - Complete - Dark

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Change Element - Complete - Dark |
| DialogType | None |
| Say | Congratulations,<br><br>Your weapon is now Dark element.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 5 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #83 · Weapon Refiner - Change Element - Complete - Phantom

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Change Element - Complete - Phantom |
| DialogType | None |
| Say | Congratulations,<br><br>Your weapon is now Phantom element.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 5 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #84 · Todo

| 字段 | 值 |
|---|---|
| Description | Todo |
| DialogType | None |
| Say | <br><br>Not Yet Added<br><br>[Exit:0] |
| Arguments | — |

### #85 · Weapon Refiner - Refine Weapon - Start

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Refine Weapon - Start |
| DialogType | Refine |
| Say | Good news, I can see that your weapon is ready to be refined.<br><br>Refinement: DC.<br>Refinement: Spell Power (MC, SC or both depending on the weapon).<br><br>Refinement: Fire Element, Ice Element, Lightning Element, Wind Element.<br>Refinement: Holy Element, Dark Element.<br>Refinement: Phantom Element.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |

### #86 · Weapon Refiner - Refine Weapon - Fail - Cannot Refine

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Refine Weapon - Fail - Cannot Refine |
| DialogType | None |
| Say | Failed to refine your weapon...<br><br><br>It is not ready to be refined.<br><br>[Exit:0] |
| Arguments | — |

### #97 · Weapon Refiner - Refine Weapon - Retreive Weapon

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Refine Weapon - Retreive Weapon |
| DialogType | RefineRetrieve |
| Say | Here's the list of weapons you are currently refineing,<br><br>If you don't see it, then you must have already received it.<br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |

### #98 · Essentials Main - Castle

| 字段 | 值 |
|---|---|
| Description | Essentials Main - Castle |
| DialogType | None |
| Say | I got what you need,<br><br>My prices are just,<br><br>[Browse Essentials:1]<br>[Browse Taoist Goods:2]<br>[Browse Dark Stones:3]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |

### #99 · DarkStone BuySell - Castle

| 字段 | 值 |
|---|---|
| Description | DarkStone BuySell - Castle |
| DialogType | BuySell |
| Say | I should be getting some new stock soon!<br><br>[Browse Essentials:1]<br>[Browse Taoist Goods:2]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 16 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 5 条（明细见 [NPCType.md](NPCType.md)） |

### #100 · Amulet BuySell - Castle

| 字段 | 值 |
|---|---|
| Description | Amulet BuySell - Castle |
| DialogType | BuySell |
| Say | I should be getting some new stock soon!<br><br>[Browse Essentials:1]<br><br>[Browse Dark Stones:3]<br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 11 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 5 条（明细见 [NPCType.md](NPCType.md)） |

### #101 · Essentials BuySell - Castle

| 字段 | 值 |
|---|---|
| Description | Essentials BuySell - Castle |
| DialogType | BuySell |
| Say | I should be getting some new stock soon!<br><br><br>[Browse Taoist Goods:2]<br>[Browse Dark Stones:3]<br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 10 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 5 条（明细见 [NPCType.md](NPCType.md)） |

### #102 · Basic Potion Main - Castle

| 字段 | 值 |
|---|---|
| Description | Basic Potion Main - Castle |
| DialogType | None |
| Say | Drugs, Get your Drugs here,<br><br>You there, You look like an addict, Wanna by some potions!?<br><br>[Browse Potions:1]<br><br>[Back away slowly:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #103 · Basic Potion BuySell - Castle

| 字段 | 值 |
|---|---|
| Description | Basic Potion BuySell - Castle |
| DialogType | BuySell |
| Say | Excellent...<br><br>Now spend every penny.<br><br><br><br>[Back:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 47 条（明细见 [NPCGood.md](NPCGood.md)） |
| Types | `NPCType` × 1 条（明细见 [NPCType.md](NPCType.md)） |

### #104 · II Teleporter

| 字段 | 值 |
|---|---|
| Description | II Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Numa Village:1] 1,000 Gold<br><br><br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #105 · NV 1K

| 字段 | 值 |
|---|---|
| Description | NV 1K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Numa Village (#37) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #106 · II 50K

| 字段 | 值 |
|---|---|
| Description | II 50K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Infernal Island (#107) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #107 · Teleport to Infernal Island

| 字段 | 值 |
|---|---|
| Description | Teleport to Infernal Island |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #108 · PK Fail - Text

| 字段 | 值 |
|---|---|
| Description | PK Fail - Text |
| DialogType | None |
| Say | Leave this instant.<br><br>I do not want to get blood all over my products.<br><br>[Exit:0] |
| Arguments | — |

### #109 · PK Fail - Teleport

| 字段 | 值 |
|---|---|
| Description | PK Fail - Teleport |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Infernal Island (#107) |
| Arguments | — |

### #110 · II Merchant

| 字段 | 值 |
|---|---|
| Description | II Merchant |
| DialogType | None |
| Say | I do no want to be seen dealing with you.<br><br>[Buy Items:1]<br>[Repair Items:2]<br><br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #111 · II Buy

| 字段 | 值 |
|---|---|
| Description | II Buy |
| DialogType | BuySell |
| Say | Faster, before someone arrives.<br><br><br><br>[Back:1]<br><br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 46 条（明细见 [NPCGood.md](NPCGood.md)） |

### #112 · II Repair

| 字段 | 值 |
|---|---|
| Description | II Repair |
| DialogType | Repair |
| Say | Quick, I think someone is coming.<br><br><br>[Back:1]<br><br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |
| Types | `NPCType` × 8 条（明细见 [NPCType.md](NPCType.md)） |

### #113 · SK1 Teleporter

| 字段 | 值 |
|---|---|
| Description | SK1 Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Inner Wall:1] 1,000 Gold<br><br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #114 · MW1 Teleporter

| 字段 | 值 |
|---|---|
| Description | MW1 Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Inner Wall:1] 1,000 Gold<br><br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #115 · Inner SK 1K

| 字段 | 值 |
|---|---|
| Description | Inner SK 1K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Inner Sabuk Keep (#116) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #116 · Teleport to Inner Sabuk Keep

| 字段 | 值 |
|---|---|
| Description | Teleport to Inner Sabuk Keep |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #117 · Inner MW 1k

| 字段 | 值 |
|---|---|
| Description | Inner MW 1k |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Inner Mud Wall (#118) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #118 · Teleport to Inner Mud Wall

| 字段 | 值 |
|---|---|
| Description | Teleport to Inner Mud Wall |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #119 · Horse Main

| 字段 | 值 |
|---|---|
| Description | Horse Main |
| DialogType | None |
| Say | Welcome to my stables.<br>Here's a selection of the horses available.<br><br>[Brown Horse:1] - 500,000 Gold, Level 15.<br>[White Horse:2] - 20,000,000 Gold, Level 42.<br>[Red Horse:3] - 100,000,000 Gold, Level 51.<br>[Black Horse:4] - 600,000,000 Gold, Level 75.<br><br>[Sell Horse:5]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 5 条（明细见 [NPCButton.md](NPCButton.md)） |

### #120 · Brown Horse - Buy

| 字段 | 值 |
|---|---|
| Description | Brown Horse - Buy |
| DialogType | None |
| Say | — |
| SuccessPage | Buy Horse Success (#124) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #121 · Buy Horse Fail - Already have horse

| 字段 | 值 |
|---|---|
| Description | Buy Horse Fail - Already have horse |
| DialogType | None |
| Say | I am unable to sell you another horse at the moment.<br><br>You currently already own a horse.<br><br>Please sell your current horse if you want to buy a new one.<br><br>[Main:1]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #122 · Buy Horse Fail - Level

| 字段 | 值 |
|---|---|
| Description | Buy Horse Fail - Level |
| DialogType | None |
| Say | I am unable to sell you this horse at the moment.<br><br>You not strong enough.<br><br>Please come back when you are higher level.<br><br>[Main:1]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #123 · Buy Horse Fail - Gold

| 字段 | 值 |
|---|---|
| Description | Buy Horse Fail - Gold |
| DialogType | None |
| Say | I am unable to sell you this horse at the moment.<br><br>You cannot afford to buy this horse<br><br>Please come back when you are wealthier.<br><br>[Main:1]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #124 · Buy Horse Success

| 字段 | 值 |
|---|---|
| Description | Buy Horse Success |
| DialogType | None |
| Say | Congratulations on purchasing your new horse.<br><br>Please take good care of it.<br><br>[Exit:0] |
| Arguments | — |

### #125 · White Horse - Buy

| 字段 | 值 |
|---|---|
| Description | White Horse - Buy |
| DialogType | None |
| Say | — |
| SuccessPage | Buy Horse Success (#124) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #126 · Red Horse - Buy

| 字段 | 值 |
|---|---|
| Description | Red Horse - Buy |
| DialogType | None |
| Say | — |
| SuccessPage | Buy Horse Success (#124) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #127 · Black Horse - Buy

| 字段 | 值 |
|---|---|
| Description | Black Horse - Buy |
| DialogType | None |
| Say | — |
| SuccessPage | Buy Horse Success (#124) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |

### #129 · Horse - Sell

| 字段 | 值 |
|---|---|
| Description | Horse - Sell |
| DialogType | None |
| Say | It's sad to see another horse abandoned by its owner, My offer is as follows:<br><br>Brown Horse - 250,000 Gold.<br>White Horse - 10,00,000 Gold.<br>Red Horse - 50,000,000 Gold.<br>Black Horse - 300,000,000 Gold.<br><br>[Main:1]<br><br>[Sell Horse:2]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #130 · Brown Horse - Buy Start

| 字段 | 值 |
|---|---|
| Description | Brown Horse - Buy Start |
| DialogType | None |
| Say | Horse Contract Information:<br><br>Type: Brown Horse.<br>Cost: 500,000 Gold.<br><br>If you want to buy this horse please [Sign Here:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #131 · White Horse - Buy Start

| 字段 | 值 |
|---|---|
| Description | White Horse - Buy Start |
| DialogType | None |
| Say | Horse Contract Information:<br><br>Type: White Horse.<br>Cost: 20,000,000 Gold.<br><br>If you want to buy this horse please [Sign Here:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #132 · Red Horse - Buy Start

| 字段 | 值 |
|---|---|
| Description | Red Horse - Buy Start |
| DialogType | None |
| Say | Horse Contract Information:<br><br>Type: Red Horse.<br>Cost: 100,000,000 Gold.<br><br>If you want to buy this horse please [Sign Here:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #133 · Black Horse - Buy Start

| 字段 | 值 |
|---|---|
| Description | Black Horse - Buy Start |
| DialogType | None |
| Say | Horse Contract Information:<br><br>Type: Black Horse.<br>Cost: 600,000,000 Gold.<br><br>If you want to buy this horse please [Sign Here:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #134 · Horse Sell - None - Start

| 字段 | 值 |
|---|---|
| Description | Horse Sell - None - Start |
| DialogType | None |
| Say | I am unable to buy a horse from you.<br><br>You currently don't own any horse.<br><br>[Main:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #135 · Horse Sell - Brown - Start

| 字段 | 值 |
|---|---|
| Description | Horse Sell - Brown - Start |
| DialogType | None |
| Say | Horse Contract Information:<br><br>Type: Brown Horse.<br>Price: 250,000 Gold.<br><br>If you want to sell this horse please [Sign Here:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #136 · Horse Sell - White - Start

| 字段 | 值 |
|---|---|
| Description | Horse Sell - White - Start |
| DialogType | None |
| Say | Horse Contract Information:<br><br>Type: White Horse.<br>Price: 10,000,000 Gold.<br><br>If you want to sell this horse please [Sign Here:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #137 · Horse Sell - Red - Start

| 字段 | 值 |
|---|---|
| Description | Horse Sell - Red - Start |
| DialogType | None |
| Say | Horse Contract Information:<br><br>Type: Red Horse.<br>Price: 50,000,000 Gold.<br><br>If you want to sell this horse please [Sign Here:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #138 · Horse Sell - Black - Start

| 字段 | 值 |
|---|---|
| Description | Horse Sell - Black - Start |
| DialogType | None |
| Say | Horse Contract Information:<br><br>Type: Black Horse.<br>Price: 300,000,000 Gold.<br><br>If you want to sell this horse please [Sign Here:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #139 · Horse Sell - Unknown - Start

| 字段 | 值 |
|---|---|
| Description | Horse Sell - Unknown - Start |
| DialogType | None |
| Say | Horse Contract Information:<br><br>Type: Unknown Horse.<br>Cost: Your Soul...<br><br>If you want to sell this horse please [Sign Here:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #140 · Horse Sell - Brown

| 字段 | 值 |
|---|---|
| Description | Horse Sell - Brown |
| DialogType | None |
| Say | — |
| SuccessPage | Sell Horse Success (#141) |
| Arguments | — |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #141 · Sell Horse Success

| 字段 | 值 |
|---|---|
| Description | Sell Horse Success |
| DialogType | None |
| Say | You've sold your horse.<br><br>It no longer loves you, Not even if you feed it a carrot.<br><br><br>[Main:1]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #142 · Horse Sell - White

| 字段 | 值 |
|---|---|
| Description | Horse Sell - White |
| DialogType | None |
| Say | — |
| SuccessPage | Sell Horse Success (#141) |
| Arguments | — |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #143 · Horse Sell - Red

| 字段 | 值 |
|---|---|
| Description | Horse Sell - Red |
| DialogType | None |
| Say | — |
| SuccessPage | Sell Horse Success (#141) |
| Arguments | — |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #144 · Horse Sell - Black

| 字段 | 值 |
|---|---|
| Description | Horse Sell - Black |
| DialogType | None |
| Say | — |
| SuccessPage | Sell Horse Success (#141) |
| Arguments | — |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #146 · Teleport Fail Level

| 字段 | 值 |
|---|---|
| Description | Teleport Fail Level |
| DialogType | None |
| Say | Failed to teleport to destination,<br><br>Not High Enough Level.<br><br>[Exit:0] |
| Arguments | — |

### #147 · Notice Board

| 字段 | 值 |
|---|---|
| Description | Notice Board |
| DialogType | None |
| Say | ... |
| Arguments | — |

### #148 · Companion Main

| 字段 | 值 |
|---|---|
| Description | Companion Main |
| DialogType | None |
| Say | Want to adopt a lovable companion?<br><br>They will help you pick up items whilst you hunt.<br><br>[Manage Pets:1]<br>[Buy Pet Food:2]<br><br>[Exit:0]<br> |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #149 · Companion Manage

| 字段 | 值 |
|---|---|
| Description | Companion Manage |
| DialogType | CompanionManage |
| Say | Please take your time,<br><br><br>[Back:1]<br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #150 · Companion BuySell

| 字段 | 值 |
|---|---|
| Description | Companion BuySell |
| DialogType | BuySell |
| Say | I have all of the pet food your lovable pet could ever want.<br><br><br>[Back:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |
| Goods | `NPCGood` × 12 条（明细见 [NPCGood.md](NPCGood.md)） |

### #151 · Marriage Main

| 字段 | 值 |
|---|---|
| Description | Marriage Main |
| DialogType | None |
| Say | Hello, How can I help someone such as yourself.<br><br>[Get Married:1]<br>[Get Devorced:2]<br>[Remove Wedding Ring:3]<br>[Make Wedding Ring:4]<br><br>[Rebirth:5]<br><br>[Exit:0]<br> |
| Arguments | — |
| Buttons | `NPCButton` × 5 条（明细见 [NPCButton.md](NPCButton.md)） |

### #152 · Marriage - Request Start

| 字段 | 值 |
|---|---|
| Description | Marriage - Request Start |
| DialogType | None |
| Say | Please face your partner...<br><br>Let me know when you are ready.<br><br>I will charge both of you 500,000 gold for a successful marriage.<br><br>[Continue:1]<br><br>[Back:2] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #153 · Marriage - Divorce Start

| 字段 | 值 |
|---|---|
| Description | Marriage - Divorce Start |
| DialogType | None |
| Say | Are you sure you want to get Divorced?<br><br>It will cost you 1,000,000 Gold.<br><br>[Proceed:1]<br><br>[Main:2] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #154 · Marriage - Remove Ring Start

| 字段 | 值 |
|---|---|
| Description | Marriage - Remove Ring Start |
| DialogType | None |
| Say | Are you sure you want to remove your wedding ring?<br><br>It will cost you 200,000 Gold.<br><br>[Remove Ring:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 2 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #155 · Marriage - Make Ring

| 字段 | 值 |
|---|---|
| Description | Marriage - Make Ring |
| DialogType | WeddingRing |
| Say | I understand that you have a ring in your possession that you want to make as your wedding ring.<br><br>Please show me the ring you would like to use.<br><br>[Back:1]<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 2 条（明细见 [NPCCheck.md](NPCCheck.md)） |

### #156 · Marriage - Request End

| 字段 | 值 |
|---|---|
| Description | Marriage - Request End |
| DialogType | None |
| Say | — |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #157 · Marriage Fail - Gold

| 字段 | 值 |
|---|---|
| Description | Marriage Fail - Gold |
| DialogType | None |
| Say | You do not have enough gold to use this service.<br><br>[Exit:0] |
| Arguments | — |

### #160 · Marriage - Divorce End

| 字段 | 值 |
|---|---|
| Description | Marriage - Divorce End |
| DialogType | None |
| Say | — |
| Arguments | — |
| Checks | `NPCCheck` × 2 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #161 · Marriage - Remove Ring End

| 字段 | 值 |
|---|---|
| Description | Marriage - Remove Ring End |
| DialogType | None |
| Say | — |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #162 · Marriage Fail - Already Married

| 字段 | 值 |
|---|---|
| Description | Marriage Fail - Already Married |
| DialogType | None |
| Say | I am unable to provide you with the service you requested.<br><br>You are already married.<br><br>[Exit:0] |
| Arguments | — |

### #163 · Marriage Fail - Not Married

| 字段 | 值 |
|---|---|
| Description | Marriage Fail - Not Married |
| DialogType | None |
| Say | I am unable to provide you with the service you requested.<br><br>You are not married.<br><br>[Exit:0] |
| Arguments | — |

### #164 · Marriage Fail - No Ring

| 字段 | 值 |
|---|---|
| Description | Marriage Fail - No Ring |
| DialogType | None |
| Say | I am unable to provide you with the service you requested.<br><br>You do not currently have a wedding ring.<br><br>[Exit:0] |
| Arguments | — |

### #165 · Marriage Fail - Already Have Ring

| 字段 | 值 |
|---|---|
| Description | Marriage Fail - Already Have Ring |
| DialogType | None |
| Say | I am unable to provide you with the service you requested.<br><br>You already have a wedding ring.<br><br>[Exit:0] |
| Arguments | — |

### #166 · Weapon Refiner - About Refine

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - About Refine |
| DialogType | None |
| Say | You seek knowledge on the refinement process?<br><br>Your weapon needs to be ready for refine.<br>You can use upto 5 Black Iron Ore, Higher the purity the higher the success rate.<br>You can use upto 3 Accessories, Higher the level and quality the higher the succes rate.<br>Duration, The longer the refine time, the higher the success rate.<br><br>[Main:1]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #167 · Weapon Refiner - Refinement Stone

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Refinement Stone |
| DialogType | RefinementStone |
| Say | If you want to make a refinement stone. You will need the following:<br><br>1x Crystal<br>2x Gold Ore<br>4x Diamond<br>4x Silver Ore<br>4x Iron Ore<br>Gold... As much or as little as you like, Higher is always better!<br><br>[Main:1]<br>[Exit:0]<br> |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #168 · Weapon Refiner - Master Refine

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Master Refine |
| DialogType | MasterRefine |
| Say | You will need the following:<br><br>Fragment (I) x10<br>Fragment (II) x10<br>Fragment (III) x1 ~ x1000<br>Refinement Stone x1<br><br>The process will either Add or Remove 5 of your chosen stat type.<br>You cannot break your weapon.<br><br>[Main:1]<br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #169 · Weapon Refiner - Weapon Reset

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Weapon Reset |
| DialogType | None |
| Say | Hello,  So you are looking to reset your weapons level?<br><br>This process takes 24 hours and has 100% success rate.<br>There's a chance to keep some of the stats gained refining.<br>The cost to reset your weapon is 1,000,000 Gold and 1x Refinement Stone.<br><br>[Reset Weapon:1]<br><br>[Back:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #170 · Item Fragment - Main

| 字段 | 值 |
|---|---|
| Description | Item Fragment - Main |
| DialogType | None |
| Say | Hello, Welcome... How can I help you?<br><br>[Fragment Items:1]<br><br>[Exchange Frament:2] :- 100x Fragment + 20,000 Gold = 1x Fragment (II)<br>[Exchange Frament (II):3]  :- 25x Fragment (II) + 50,000 Gold = 1x Fragment (III)<br><br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |

### #171 · Item Fragment - Fragment

| 字段 | 值 |
|---|---|
| Description | Item Fragment - Fragment |
| DialogType | ItemFragment |
| Say | Please show me the item you want to fragment.<br><br>I can break down, Weapons, Armours, Helmets, Necklaces, Bracelets, Rings and Shoes.<br><br>Common Items will yield Fragments<br>Superiour and Elite Items will result in Fragment (II)s.<br><br>[Back:1]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #172 · Item Fragment - Upgrade Fragment

| 字段 | 值 |
|---|---|
| Description | Item Fragment - Upgrade Fragment |
| DialogType | None |
| Say | — |
| SuccessPage | Item Fragment - Main (#170) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |

### #173 · Item Fragment - Upgrade Fragment (II)

| 字段 | 值 |
|---|---|
| Description | Item Fragment - Upgrade Fragment (II) |
| DialogType | None |
| Say | — |
| SuccessPage | Item Fragment - Main (#170) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |

### #174 · Item Fragment - Fail - Gold

| 字段 | 值 |
|---|---|
| Description | Item Fragment - Fail - Gold |
| DialogType | None |
| Say | I cannot upgrade your fragments.<br><br><br>Not enough Money.<br><br><br>[Main:1]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #175 · Item Fragment - Fail - Not enough Fragments

| 字段 | 值 |
|---|---|
| Description | Item Fragment - Fail - Not enough Fragments |
| DialogType | None |
| Say | I cannot upgrade your fragments.<br><br>Not enough Fragments.<br><br><br>[Main:1]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #176 · Item Fragment - Fail - Not enough Fragment (II)s

| 字段 | 值 |
|---|---|
| Description | Item Fragment - Fail - Not enough Fragment (II)s |
| DialogType | None |
| Say | I cannot upgrade your fragments.<br><br>Not enough Fragments (II).<br><br><br>[Main:1]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #177 · Item Fragment - Fail - Not enough Inventory Room

| 字段 | 值 |
|---|---|
| Description | Item Fragment - Fail - Not enough Inventory Room |
| DialogType | None |
| Say | I cannot upgrade your fragments.<br><br>Not enough space in your inventory.<br><br><br>[Main:1]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #178 · Weapon Refiner - Reset Start

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Reset Start |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Refine Weapon - Retreive Weapon (#97) |
| Arguments | — |
| Checks | `NPCCheck` × 6 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |

### #179 · Weapon Refiner - Fail - Not Leveled

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Fail - Not Leveled |
| DialogType | None |
| Say | Your weapon has not leveled.<br><br>Please come back when you have a weapon that's leveled.<br><br><br>[Exit:0] |
| Arguments | — |

### #180 · Weapon Refiner - Fail - Reset Cooldown

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Fail - Reset Cooldown |
| DialogType | None |
| Say | Your weapon has recently been reset.<br><br>Please come back when the reset cooldown has finished<br><br><br>[Exit:0] |
| Arguments | — |

### #181 · Weapon Refiner - Fail - No Refinement Stone

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Fail - No Refinement Stone |
| DialogType | None |
| Say | You do not have a Refinement Stone.<br><br>Please come back when you have a Refinement Stone.<br><br><br>[Exit:0] |
| Arguments | — |

### #182 · TT Teleporter

| 字段 | 值 |
|---|---|
| Description | TT Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Lost Paradise:1] 10,000 Gold<br><br><br><br><br>[Freedom Pass:2] [Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #183 · TT 10K

| 字段 | 值 |
|---|---|
| Description | TT 10K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Taoist Temple (#184) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #184 · Teleport to Taoist Temple

| 字段 | 值 |
|---|---|
| Description | Teleport to Taoist Temple |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #186 · LP 20K

| 字段 | 值 |
|---|---|
| Description | LP 20K |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Lost Paradise (#32) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #187 · FV 20K and Level 45+

| 字段 | 值 |
|---|---|
| Description | FV 20K and Level 45+ |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Frost Village (#189) |
| Arguments | — |
| Checks | `NPCCheck` × 2 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #189 · Teleport to Frost Village

| 字段 | 值 |
|---|---|
| Description | Teleport to Frost Village |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #190 · FV Teleporter

| 字段 | 值 |
|---|---|
| Description | FV Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Lost Paradise:1] 20,000 Gold<br><br>[Holy Palace:2] 30,000 Gold<br><br><br>[Freedom Pass:3] [Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |

### #191 · BC Teleporter

| 字段 | 值 |
|---|---|
| Description | BC Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Bichon Town:1] 10,000 Gold<br><br><br><br><br>[Freedom Pass:2] [Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #192 · Teleport to Bichon Castle

| 字段 | 值 |
|---|---|
| Description | Teleport to Bichon Castle |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #193 · BC 10K and Level 45+

| 字段 | 值 |
|---|---|
| Description | BC 10K and Level 45+ |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Bichon Castle (#192) |
| Arguments | — |
| Checks | `NPCCheck` × 2 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #194 · Accessory Refiner - Main

| 字段 | 值 |
|---|---|
| Description | Accessory Refiner - Main |
| DialogType | None |
| Say | So you're looking to upgrade your accessory...<br><br>[About:1] Accessory upgrading.<br><br>[Level Up:2] Accessory.<br>[Upgrade:3] Accessory.<br><br>[Reset:4] Accessory.<br><br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 4 条（明细见 [NPCButton.md](NPCButton.md)） |

### #195 · Accessory Refiner - About

| 字段 | 值 |
|---|---|
| Description | Accessory Refiner - About |
| DialogType | None |
| Say | In order to upgrade an accessory you first must level it up.<br><br>You melt down the same type of accessory until the accessory has leveled.<br><br>Once the accessory has leveled you can then choose what to upgrade.<br><br>[Main:1]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #196 · Accessory Refiner - Level Up

| 字段 | 值 |
|---|---|
| Description | Accessory Refiner - Level Up |
| DialogType | AccessoryRefineLevel |
| Say | First Select the accessory that you want to level up.<br><br>Then select other assessories that you want to melt down and use.<br><br><br>[Main:1]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #197 · Accessory Refiner - Upgrade

| 字段 | 值 |
|---|---|
| Description | Accessory Refiner - Upgrade |
| DialogType | AccessoryRefineUpgrade |
| Say | Show me your accessory that has leveled up and is ready for upgrade.<br><br>The process cannot fail, so do not worry.<br><br><br>[Main:1]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #198 · Rusty Accessory Main

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory Main |
| DialogType | None |
| Say | Welcome!<br><br>[Ask:1] About Numa Rusty Accessories<br><br>[Ask:2] About Lair Accessories.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #200 · Rusty Accessory - Numa Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Numa Start |
| DialogType | None |
| Say | The Cleaning fee is 1,000,000 but the process rarely works because of how old the items are.<br><br>[Rusty Signet Of Myrmidon:1]<br>[Rusty Signet Of Evoker:2]<br>[Rusty Signet Of Vicar:3]<br>[Rusty Charm Of The Destroyer:4]<br>[Rusty Amulet Of Dark Sorcery:5]<br>[Rusty Pendant Of Purification:6]<br>[Rusty Bracer Of Revelation:7]<br>[Rusty Ring Of Enlightenment:8]<br>[Rusty Bracelet Of Ascension:9]<br> |
| Arguments | — |
| Buttons | `NPCButton` × 9 条（明细见 [NPCButton.md](NPCButton.md)） |

### #201 · Rusty Accessory - Signet Of Myrmidon - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Signet Of Myrmidon - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Signet Of Myrmidon - End (#202) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #202 · Rusty Accessory - Signet Of Myrmidon - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Signet Of Myrmidon - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your ring has survive the clearning process, You can now go and Upgrade the accessory in Bichon Town.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #203 · Rusty Accessory - Numa - Fail - No Item

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Numa - Fail - No Item |
| DialogType | None |
| Say | I am unable to find the rusty item.<br><br>[Retry:1]<br><br>[Main:2]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #204 · Rusty Accessory - Numa - Fail - No Room

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Numa - Fail - No Room |
| DialogType | None |
| Say | You do not have inventory space.<br><br>[Retry:1]<br><br>[Main:2]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #205 · Rusty Accessory - Numa - Fail - No Gold

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Numa - Fail - No Gold |
| DialogType | None |
| Say | You cannot aford the cost of the cleaning service.<br><br>[Retry:1]<br><br>[Main:2]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #206 · Rusty Accessory - Numa - Fail - Break

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Numa - Fail - Break |
| DialogType | None |
| Say | I am sorry, The ring was too old to survive the cleaning process.<br><br>[Retry:1]<br><br>[Main:2]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #207 · Rusty Accessory - Signet Of Evoker - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Signet Of Evoker - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Signet Of Evoker - End (#208) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #208 · Rusty Accessory - Signet Of Evoker - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Signet Of Evoker - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your ring has survive the clearning process, You can now go and Upgrade the accessory in Bichon Town.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #209 · Rusty Accessory - Signet Of Vicar - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Signet Of Vicar - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Signet Of Vicar - End (#210) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #210 · Rusty Accessory - Signet Of Vicar - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Signet Of Vicar - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your ring has survive the clearning process, You can now go and Upgrade the accessory in Bichon Town.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #211 · Rusty Accessory - Charm Of The Destroyer - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Charm Of The Destroyer - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Charm Of The Destroyer - End (#212) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #212 · Rusty Accessory - Charm Of The Destroyer - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Charm Of The Destroyer - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your ring has survive the clearning process, You can now go and Upgrade the accessory in Bichon Town.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #213 · Rusty Accessory - Amulet Of Dark Sorcery - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Amulet Of Dark Sorcery - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Amulet Of Dark Sorcery - End (#214) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #214 · Rusty Accessory - Amulet Of Dark Sorcery - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Amulet Of Dark Sorcery - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your ring has survive the clearning process, You can now go and Upgrade the accessory in Bichon Town.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #220 · Rusty Accessory - Pendant Of Purification - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Pendant Of Purification - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Pendant Of Purification - End (#221) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #221 · Rusty Accessory - Pendant Of Purification - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Pendant Of Purification - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your ring has survive the clearning process, You can now go and Upgrade the accessory in Bichon Town.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #222 · Rusty Accessory - Bracer Of Revelation - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Bracer Of Revelation - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Bracer Of Revelation - End (#223) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #223 · Rusty Accessory - Bracer Of Revelation - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Bracer Of Revelation - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your ring has survive the clearning process, You can now go and Upgrade the accessory in Bichon Town.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #224 · Rusty Accessory - Ring Of Enlightenment - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Ring Of Enlightenment - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Ring Of Enlightenment - End (#225) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #225 · Rusty Accessory - Ring Of Enlightenment - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Ring Of Enlightenment - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your ring has survive the clearning process, You can now go and Upgrade the accessory in Bichon Town.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #226 · Rusty Accessory - Bracelet Of Ascension - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Bracelet Of Ascension - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Bracelet Of Ascension - End (#227) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #227 · Rusty Accessory - Bracelet Of Ascension - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Bracelet Of Ascension - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your ring has survive the clearning process, You can now go and Upgrade the accessory in Bichon Town.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #228 · Weapon Refiner - Fail - Not Max Level

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Fail - Not Max Level |
| DialogType | None |
| Say | Your weapon has not reached the maximum level.<br><br>Please come back when you have a weapon that's fully leveled up.<br><br><br>[Exit:0] |
| Arguments | — |

### #229 · HP 30K and Lv 45+

| 字段 | 值 |
|---|---|
| Description | HP 30K and Lv 45+ |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Holy Palace (#230) |
| Arguments | — |
| Checks | `NPCCheck` × 2 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #230 · Teleport to Holy Palace

| 字段 | 值 |
|---|---|
| Description | Teleport to Holy Palace |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #231 · MS Freedom Pass

| 字段 | 值 |
|---|---|
| Description | MS Freedom Pass |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Mystery Ship (#232) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #232 · Teleport to Mystery Ship

| 字段 | 值 |
|---|---|
| Description | Teleport to Mystery Ship |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #233 · LA Freedom Pass and Lv 45+

| 字段 | 值 |
|---|---|
| Description | LA Freedom Pass and Lv 45+ |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Lava Area  (#234) |
| Arguments | — |
| Checks | `NPCCheck` × 2 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #234 · Teleport to Lava Area 

| 字段 | 值 |
|---|---|
| Description | Teleport to Lava Area  |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #235 · BC Freedom Pass and Lv 45+

| 字段 | 值 |
|---|---|
| Description | BC Freedom Pass and Lv 45+ |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Banyo Cave (#236) |
| Arguments | — |
| Checks | `NPCCheck` × 2 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #236 · Teleport to Banyo Cave

| 字段 | 值 |
|---|---|
| Description | Teleport to Banyo Cave |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #237 · Teleport Fail Freedom Pass

| 字段 | 值 |
|---|---|
| Description | Teleport Fail Freedom Pass |
| DialogType | None |
| Say | Failed to teleport to destination,<br><br>No Freedom Pass.<br><br>[Exit:0] |
| Arguments | — |

### #238 · Freedom Teleporter

| 字段 | 值 |
|---|---|
| Description | Freedom Teleporter |
| DialogType | None |
| Say | Where would you like to teleport to?<br><br>[Mystery Ship:1] Freedom Pass<br><br>[Lava Area:2] Freedom Pass (Level 45+)<br><br>[Banyo Cave:3] Freedom Pass (Level 45+)<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 3 条（明细见 [NPCButton.md](NPCButton.md)） |

### #239 · Rusty Accessory - Lair Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Lair Start |
| DialogType | None |
| Say | The Forging fee is 2,000,000 but the process rarely works because of how old the items are.<br><br>[Seal Of Overlord:1]<br>[Bracelet Of Overlord:2]<br>[Medallion Of Overlord:3]<br>[Arcanist's Band Of Dignity:4]<br>[Arcanist's Bracelet Of Dignity:5]<br>[Arcanist's Amulet Of Dignity:6]<br>[Hierophant's Signet Of Moon:7]<br>[Hierophant's Bracer Of Moon:8]<br>[Hierophant's Pendant Of Moon:9]<br> |
| Arguments | — |
| Buttons | `NPCButton` × 9 条（明细见 [NPCButton.md](NPCButton.md)） |

### #240 · Rusty Accessory - Lair - Fail - No Rings

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Lair - Fail - No Rings |
| DialogType | None |
| Say | I am unable to find the items.<br><br>You need Rusty, Cracked and Worn Rings<br><br>[Retry:1]<br><br>[Main:2]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #241 · Rusty Accessory - Lair - Fail - No Bracelets

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Lair - Fail - No Bracelets |
| DialogType | None |
| Say | I am unable to find the items.<br><br>You need Rusty, Cracked, Scracted and Worn Bracelets<br><br><br>[Retry:1]<br><br>[Main:2]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #242 · Rusty Accessory - Lair - Fail - No Necklace

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Lair - Fail - No Necklace |
| DialogType | None |
| Say | I am unable to find the items.<br><br>You need Rusty, Cracked and Worn Necklaces<br><br>[Retry:1]<br><br>[Main:2]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #243 · Rusty Accessory - Lair - Fail - No Room

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Lair - Fail - No Room |
| DialogType | None |
| Say | You do not have inventory space.<br><br>[Retry:1]<br><br>[Main:2]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #244 · Rusty Accessory - Lair - Fail - No Gold

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Lair - Fail - No Gold |
| DialogType | None |
| Say | You cannot aford the cost of the cleaning service.<br><br>[Retry:1]<br><br>[Main:2]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #245 · Rusty Accessory - Lair - Fail - Break

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Lair - Fail - Break |
| DialogType | None |
| Say | I am sorry, The items was too old and damaged.<br><br>[Retry:1]<br><br>[Main:2]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #246 · Rusty Accessory - Seal Of Overlord - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Seal Of Overlord - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Seal Of Overlord - End (#247) |
| Arguments | — |
| Checks | `NPCCheck` × 5 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 4 条（明细见 [NPCAction.md](NPCAction.md)） |

### #247 · Rusty Accessory - Seal Of Overlord - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Seal Of Overlord - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your ring has survive the forging process.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #248 · Rusty Accessory - Bracelet Of Overlord - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Bracelet Of Overlord - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Bracelet Of Overlord - End (#249) |
| Arguments | — |
| Checks | `NPCCheck` × 6 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #249 · Rusty Accessory - Bracelet Of Overlord - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Bracelet Of Overlord - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your bracelet has survive the forging process.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #251 · Rusty Accessory - Medallion Of Overlord - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Medallion Of Overlord - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Medallion Of Overlord - End (#252) |
| Arguments | — |
| Checks | `NPCCheck` × 5 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 4 条（明细见 [NPCAction.md](NPCAction.md)） |

### #252 · Rusty Accessory - Medallion Of Overlord - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Medallion Of Overlord - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your Necklace has survive the forging process.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #254 · Rusty Accessory - Arcanist's Band Of Dignity - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Arcanist's Band Of Dignity - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Arcanist's Band Of Dignity - End (#255) |
| Arguments | — |
| Checks | `NPCCheck` × 5 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 4 条（明细见 [NPCAction.md](NPCAction.md)） |

### #255 · Rusty Accessory - Arcanist's Band Of Dignity - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Arcanist's Band Of Dignity - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your ring has survive the forging process.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #256 · Rusty Accessory - Arcanist's Bracelet Of Dignity - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Arcanist's Bracelet Of Dignity - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Arcanist's Bracelet Of Dignity - End (#257) |
| Arguments | — |
| Checks | `NPCCheck` × 6 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #257 · Rusty Accessory - Arcanist's Bracelet Of Dignity - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Arcanist's Bracelet Of Dignity - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your bracelet has survive the forging process.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #258 · Rusty Accessory - Arcanist's Amulet Of Dignity - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Arcanist's Amulet Of Dignity - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Arcanist's Amulet Of Dignity - End (#259) |
| Arguments | — |
| Checks | `NPCCheck` × 5 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 4 条（明细见 [NPCAction.md](NPCAction.md)） |

### #259 · Rusty Accessory - Arcanist's Amulet Of Dignity - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Arcanist's Amulet Of Dignity - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your Necklace has survive the forging process.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #260 · Rusty Accessory - Hierophant's Signet Of Moon - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Hierophant's Signet Of Moon - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Hierophant's Signet Of Moon - End (#261) |
| Arguments | — |
| Checks | `NPCCheck` × 5 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 4 条（明细见 [NPCAction.md](NPCAction.md)） |

### #261 · Rusty Accessory - Hierophant's Signet Of Moon - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Hierophant's Signet Of Moon - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your ring has survive the forging process.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #262 · Rusty Accessory - Hierophant's Bracer Of Moon - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Hierophant's Bracer Of Moon - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Hierophant's Bracer Of Moon - End (#263) |
| Arguments | — |
| Checks | `NPCCheck` × 6 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #263 · Rusty Accessory - Hierophant's Bracer Of Moon - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Hierophant's Bracer Of Moon - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your bracelet has survive the forging process.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #264 · Rusty Accessory - Hierophant's Pendant Of Moon - Start

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Hierophant's Pendant Of Moon - Start |
| DialogType | None |
| Say | — |
| SuccessPage | Rusty Accessory - Hierophant's Pendant Of Moon - End (#265) |
| Arguments | — |
| Checks | `NPCCheck` × 5 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 4 条（明细见 [NPCAction.md](NPCAction.md)） |

### #265 · Rusty Accessory - Hierophant's Pendant Of Moon - End

| 字段 | 值 |
|---|---|
| Description | Rusty Accessory - Hierophant's Pendant Of Moon - End |
| DialogType | None |
| Say | Congratulations,<br><br>Your Necklace has survive the forging process.<br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #266 · Accessory Refiner - Reset

| 字段 | 值 |
|---|---|
| Description | Accessory Refiner - Reset |
| DialogType | AccessoryReset |
| Say | Show me your accessory that has been leveled up and is ready to be reset.<br><br>The process cannot fail, so do not worry.<br><br><br>[Main:1]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #267 · Weapon Craft Main

| 字段 | 值 |
|---|---|
| Description | Weapon Craft Main |
| DialogType | None |
| Say | ...<br><br><br>[Craft Weapon:1]<br><br>[Upgrade Trinkets:2]<br><br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #268 · Weapon Craft - Craft

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Craft |
| DialogType | WeaponCraft |
| Say | ...<br><br>Each Jewel will add a stat to the weapon.<br><br>Higher quality Jewels will result in more stats.<br><br>[Back:1]<br><br>[Main:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #269 · Weapon Craft - Upgrade

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Upgrade |
| DialogType | None |
| Say | Upgrade will need Component x100 and 100,000 Gold.<br><br>[Yellow Orb to Yellow Trinket:20] -- [Yellow Trinket to Yellow Cube:21]<br>[Blue Orb to Blue Trinket:30] -- [Blue Trinket to Blue Cube:31]<br>[Red Orb to Red Trinket:40] -- [Red Trinket to Red Cube:41]<br>[Purple Orb to Purple Trinket:50] -- [Purple Trinket to Purple Cube:51]<br>[Green Orb to Green Trinket:60] -- [Green Trinket to Green Cube:61]<br>[Grey Orb to Grey Trinket:70] -- [Grey Trinket to Grey Cube:71]<br><br>[Back:1]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 13 条（明细见 [NPCButton.md](NPCButton.md)） |

### #270 · Weapon Craft - Failed - Not enough Items

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Failed - Not enough Items |
| DialogType | None |
| Say | Failed to craft item.<br><br>Not enough items.<br><br>[Main:1]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #271 · Weapon Craft - Failed - Not enough Gold

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Failed - Not enough Gold |
| DialogType | None |
| Say | Failed to craft item.<br><br>Not enough gold.<br><br>[Main:1]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #272 · Weapon Craft - Failed - Not enough Space

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Failed - Not enough Space |
| DialogType | None |
| Say | Failed to craft item.<br><br>Not enough inventory space.<br><br>[Main:1]<br><br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #274 · Weapon Craft - Yellow Orb

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Yellow Orb |
| DialogType | None |
| Say | Your Item has been crafted successfully.<br><br><br><br>[Again:2]<br><br>[Main:1]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #275 · Weapon Craft - Yellow Trinket

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Yellow Trinket |
| DialogType | None |
| Say | Your Item has been crafted successfully.<br><br><br><br>[Again:2]<br><br>[Main:1]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #276 · Weapon Craft - Blue Orb

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Blue Orb |
| DialogType | None |
| Say | Your Item has been crafted successfully.<br><br><br><br>[Again:2]<br><br>[Main:1]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #277 · Weapon Craft - Blue Trinket

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Blue Trinket |
| DialogType | None |
| Say | Your Item has been crafted successfully.<br><br><br><br>[Again:2]<br><br>[Main:1]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #278 · Weapon Craft - Red Orb

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Red Orb |
| DialogType | None |
| Say | Your Item has been crafted successfully.<br><br><br><br>[Again:2]<br><br>[Main:1]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #279 · Weapon Craft - Red Trinket

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Red Trinket |
| DialogType | None |
| Say | Your Item has been crafted successfully.<br><br><br><br>[Again:2]<br><br>[Main:1]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #280 · Weapon Craft - Purple Orb

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Purple Orb |
| DialogType | None |
| Say | Your Item has been crafted successfully.<br><br><br><br>[Again:2]<br><br>[Main:1]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #281 · Weapon Craft - Purple Trinket

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Purple Trinket |
| DialogType | None |
| Say | Your Item has been crafted successfully.<br><br><br><br>[Again:2]<br><br>[Main:1]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #282 · Weapon Craft - Green Orb

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Green Orb |
| DialogType | None |
| Say | Your Item has been crafted successfully.<br><br><br><br>[Again:2]<br><br>[Main:1]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #283 · Weapon Craft - Green Trinket

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Green Trinket |
| DialogType | None |
| Say | Your Item has been crafted successfully.<br><br><br><br>[Again:2]<br><br>[Main:1]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #284 · Weapon Craft - Grey Orb

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Grey Orb |
| DialogType | None |
| Say | Your Item has been crafted successfully.<br><br><br><br>[Again:2]<br><br>[Main:1]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #285 · Weapon Craft - Grey Trinket

| 字段 | 值 |
|---|---|
| Description | Weapon Craft - Grey Trinket |
| DialogType | None |
| Say | Your Item has been crafted successfully.<br><br><br><br>[Again:2]<br><br>[Main:1]<br><br><br><br>[Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 3 条（明细见 [NPCAction.md](NPCAction.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #286 · LL Teleporter

| 字段 | 值 |
|---|---|
| Description | LL Teleporter |
| DialogType | None |
| Say | Select Destination<br><br>[Banya Island:1] 10,000 Gold (Level 25+)<br><br><br><br>[Freedom Pass:2] [Exit:0] |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #287 · LL 30K and Level 35+

| 字段 | 值 |
|---|---|
| Description | LL 30K and Level 35+ |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Lost Land (#288) |
| Arguments | — |
| Checks | `NPCCheck` × 2 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #288 · Teleport to Lost Land

| 字段 | 值 |
|---|---|
| Description | Teleport to Lost Land |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #289 · Emblem Main

| 字段 | 值 |
|---|---|
| Description | Emblem Main |
| DialogType | None |
| Say | Emblems are expensive to make...<br><br><br>[Buy:1] an Emblem<br><br><br><br>[Exit:0] <br> |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |
| Types | `NPCType` × 1 条（明细见 [NPCType.md](NPCType.md)） |

### #290 · Emblem Buy Sell

| 字段 | 值 |
|---|---|
| Description | Emblem Buy Sell |
| DialogType | BuySell |
| Say | Take your time...<br><br><br><br><br>[Back:1]<br><br>[Exit:0] |
| Arguments | — |
| Goods | `NPCGood` × 3 条（明细见 [NPCGood.md](NPCGood.md)） |

### #291 · Weapon Refiner - Special Refine - Main

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Main |
| DialogType | None |
| Say | Special Refine requires:<br><br>Oil of Conservation x1<br>Oil of The War God x1<br><br>[Fragment (III):1] x30 + Crystal x 30<br>[Fragment (III):2] x60 + Crystal x 50 <br>[Fragment (III):3] x100 + Crystal x 50 <br><br>[Main:4]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 4 条（明细见 [NPCButton.md](NPCButton.md)） |

### #292 · Weapon Refiner - Special Refine - 30

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - 30 |
| DialogType | None |
| Say | For 30 Fragments I can offer:<br><br>[Health:1] + 75    (Limit: +1250)<br>[Mana:2] +40    (Limit: +850)<br>[Accuracy:3] +3    (Limit: +30)<br>[Agility:4] +3    (Limit: +30)<br>[AC:5] 0-5    (Limit: +50)<br>[MR:6] 0-5    (Limit: +50)<br><br>[Main:7]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 7 条（明细见 [NPCButton.md](NPCButton.md)） |

### #293 · Weapon Refiner - Special Refine - 60

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - 60 |
| DialogType | None |
| Say | For 60 Fragments I can offer:<br><br>[Critical Chance:1] +1    (Limit: +25)<br>[Critical Damage:2] +5    (Limit: +200)<br>[Attack Speed:3] +3    (Limit: +15)<br>[Life Steal:4] +3    (Limit: +21)<br><br><br><br>[Main:5]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 5 条（明细见 [NPCButton.md](NPCButton.md)） |

### #294 · Weapon Refiner - Special Refine - 100

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - 100 |
| DialogType | None |
| Say | For 100 Fragments I can offer:<br><br>[Paralysis:1] +1% (PvP and PvE - 2 Seconds)    (Limit: +13)<br>[Slow:2] +1% (PvE) - 10 Seconds    (Limit: +15)<br>[Silence:3] +1% (PvP and PvE - 5 Seconds)    (Limit: +10)<br>[Block Chance:4] +1% (Melee)    (Limit: +10)<br>[Evasion Chance:5] + 1% (Magic)    (Limit: +10)<br><br><br>[Main:6]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 6 条（明细见 [NPCButton.md](NPCButton.md)） |

### #295 · Weapon Refiner - Special Refine - Health Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Health Q |
| DialogType | None |
| Say | Are you sure you want to add Health to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #296 · Weapon Refiner - Special Refine - Health A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Health A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #297 · Weapon Refiner - Special Refine - Mana Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Mana Q |
| DialogType | None |
| Say | Are you sure you want to add Mana to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #298 · Weapon Refiner - Special Refine - Mana A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Mana A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #300 · Weapon Refiner - Special Refine - Accuracy Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Accuracy Q |
| DialogType | None |
| Say | Are you sure you want to add Accuracy to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #301 · Weapon Refiner - Special Refine - Accuracy A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Accuracy A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #302 · Weapon Refiner - Special Refine - Agility Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Agility Q |
| DialogType | None |
| Say | Are you sure you want to add Agility to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #303 · Weapon Refiner - Special Refine - Agility A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Agility A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #304 · Weapon Refiner - Special Refine - AC Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - AC Q |
| DialogType | None |
| Say | Are you sure you want to add AC to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #305 · Weapon Refiner - Special Refine - AC A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - AC A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #306 · Weapon Refiner - Special Refine - MR Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - MR Q |
| DialogType | None |
| Say | Are you sure you want to add MR to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #307 · Weapon Refiner - Special Refine - MR A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - MR A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #308 · Weapon Refiner - Special Refine - Not enough Material

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Not enough Material |
| DialogType | None |
| Say | You lack the required materials to special refine.<br><br>[Main:1]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #309 · Weapon Refiner - Special Refine - Critical Chance Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Critical Chance Q |
| DialogType | None |
| Say | Are you sure you want to add Critical Chance to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #310 · Weapon Refiner - Special Refine - Critical Chance A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Critical Chance A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #311 · Weapon Refiner - Special Refine - Critical Damage Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Critical Damage Q |
| DialogType | None |
| Say | Are you sure you want to add Critical Damage to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #312 · Weapon Refiner - Special Refine - Critical Damage A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Critical Damage A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #313 · Weapon Refiner - Special Refine - Attack Speed Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Attack Speed Q |
| DialogType | None |
| Say | Are you sure you want to add Attack Speed to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #314 · Weapon Refiner - Special Refine - Attack Speed A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Attack Speed A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #315 · Weapon Refiner - Special Refine - Life Steal Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Life Steal Q |
| DialogType | None |
| Say | Are you sure you want to add Life Steal to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #316 · Weapon Refiner - Special Refine - Life Steal A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Life Steal A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #317 · Weapon Refiner - Special Refine - Paralysis Chance Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Paralysis Chance Q |
| DialogType | None |
| Say | Are you sure you want to add Paralysis Chance to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #318 · Weapon Refiner - Special Refine - Paralysis Chance A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Paralysis Chance A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #319 · Weapon Refiner - Special Refine - Slow Chance Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Slow Chance Q |
| DialogType | None |
| Say | Are you sure you want to add Slow Chance to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #320 · Weapon Refiner - Special Refine - Slow Chance A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Slow Chance A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #321 · Weapon Refiner - Special Refine - Silence Chance Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Silence Chance Q |
| DialogType | None |
| Say | Are you sure you want to add Silence Chance to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #322 · Weapon Refiner - Special Refine - Silence Chance A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Silence Chance A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #323 · Weapon Refiner - Special Refine - Block Chance Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Block Chance Q |
| DialogType | None |
| Say | Are you sure you want to add Block Chance to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #324 · Weapon Refiner - Special Refine - Block Chance A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Block Chance A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #325 · Weapon Refiner - Special Refine - Evasion Chance Q

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Evasion Chance Q |
| DialogType | None |
| Say | Are you sure you want to add Evasoin Chance to your weapon?<br><br>[Yes, Refine:1]<br><br>[Main:2]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |

### #326 · Weapon Refiner - Special Refine - Evasion Chance A

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Evasion Chance A |
| DialogType | None |
| Say | — |
| SuccessPage | Weapon Refiner - Special Refine - Main (#291) |
| Arguments | — |
| Checks | `NPCCheck` × 8 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 5 条（明细见 [NPCAction.md](NPCAction.md)） |

### #327 · Weapon Refiner - Special Refine - Too many Status

| 字段 | 值 |
|---|---|
| Description | Weapon Refiner - Special Refine - Too many Status |
| DialogType | None |
| Say | You cannot not add anymore of this stat to your weapon.<br><br>[Main:1]<br>[Exit:0] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #328 · WA 5M and Level 60+

| 字段 | 值 |
|---|---|
| Description | WA 5M and Level 60+ |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Western Arids (#330) |
| Arguments | — |
| Checks | `NPCCheck` × 2 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #329 · AF 5mil and Freedom Pass and Level 60+

| 字段 | 值 |
|---|---|
| Description | AF 5mil and Freedom Pass and Level 60+ |
| DialogType | None |
| Say | — |
| SuccessPage | Teleport to Arid Flats (#331) |
| Arguments | — |
| Checks | `NPCCheck` × 3 条（明细见 [NPCCheck.md](NPCCheck.md)） |
| Actions | `NPCAction` × 2 条（明细见 [NPCAction.md](NPCAction.md)） |

### #330 · Teleport to Western Arids

| 字段 | 值 |
|---|---|
| Description | Teleport to Western Arids |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #331 · Teleport to Arid Flats

| 字段 | 值 |
|---|---|
| Description | Teleport to Arid Flats |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #332 · Rebirth Main

| 字段 | 值 |
|---|---|
| Description | Rebirth Main |
| DialogType | None |
| Say | For EACH rebirth that you accept the following changes will happen:<br><br>You will need to be level  86 + Current Rebith Count.<br>When Rebirthing, you will be set to level 1 and keep 0.5% of your current experience<br>x50% More Damage in PvE<br>x20% More Damage in PvP<br>+20% More Drop Rate<br>+20% More Gold Rate<br>x50% Less Experience Gained.<br>Every death in PvE will cost you all of your experience. (PvP no Punishment)<br>[Accept:1] |
| Arguments | — |
| Buttons | `NPCButton` × 1 条（明细见 [NPCButton.md](NPCButton.md)） |

### #333 · Rebirth Act

| 字段 | 值 |
|---|---|
| Description | Rebirth Act |
| DialogType | None |
| Say | If you met the requirements You will have rebirthed. |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

### #334 · Fame Main

| 字段 | 值 |
|---|---|
| Description | Fame Main |
| DialogType | None |
| Say | I am the village chief. If you have been doing quests throughout the world you can earn rewards and titles as recognition.<br><br>{<1:Unlimited>:LawnGreen} points are required for your next title.<br><br>[Purchase Title:1]<br>[Browse Store:2]<br><br>[Close:0] |
| Arguments | — |
| Buttons | `NPCButton` × 2 条（明细见 [NPCButton.md](NPCButton.md)） |
| Values | `NPCValue` × 2 条（明细见 [NPCValue.md](NPCValue.md)） |

### #335 · Fame Check

| 字段 | 值 |
|---|---|
| Description | Fame Check |
| DialogType | None |
| Say | — |
| SuccessPage | Fame Act (#336) |
| Arguments | — |
| Checks | `NPCCheck` × 1 条（明细见 [NPCCheck.md](NPCCheck.md)） |

### #336 · Fame Act

| 字段 | 值 |
|---|---|
| Description | Fame Act |
| DialogType | None |
| Say | — |
| Arguments | — |
| Actions | `NPCAction` × 1 条（明细见 [NPCAction.md](NPCAction.md)） |

