<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改。重新生成: dotnet run --project Tools/SystemDbProbe -- --dump docs/database -->

# 传送点（MovementInfo）

> 记录 #2482 – #2787，共 554 条（第 1/2 部分）。

[README](../README.md) · [下一部分 →](MovementInfo.2.md)

## 快速浏览

| # | SourceRegion | DestinationRegion | Icon | NeedItem | RequiredClass |
|---|---|---|---|---|---|
| 2482 | 0 (#1) / Palace Entrance (#19) | 0_000 (#2) / Entrance Landing (#35) | Building | — | All |
| 2483 | 0_000 (#2) / Entrance Door (#34) | 0 (#1) / Palace Landing (#20) | Province | — | All |
| 2484 | 0_000 (#2) / Left Door (#36) | 0_001 (#3) / Landing (#41) | Building | — | All |
| 2485 | 0_000 (#2) / Right Door (#38) | 0_002 (#4) / Landing (#43) | Building | — | All |
| 2486 | 0 (#1) / North Way Entrance (#16) | E02 (#223) / Bichon Town Landing (#87) | Province | — | All |
| 2487 | E02 (#223) / Bichon Town Door (#85) | 0 (#1) / North Way Landing (#17) | Province | — | All |
| 2488 | E01 (#222) / Lost Paradise Door (#82) | 1 (#5) / North Way Landing (#54) | Province | — | All |
| 2489 | 1 (#5) / North Way Door (#53) | E01 (#222) / Lost Paradise Landing (#86) | Province | — | All |
| 2490 | E02 (#223) / Left Door (#83) | E01 (#222) / Right Landing (#81) | Province | — | All |
| 2491 | E01 (#222) / Right Door (#80) | E02 (#223) / Left Landing (#84) | Province | — | All |
| 2492 | 0 (#1) / Bug Cave Entrance (#2) | D801 (#160) / Entrance Landing (#429) | Cave | — | All |
| 2493 | D801 (#160) / Entrance Door (#428) | 0 (#1) / Bug Cave Landing (#3) | Exit | — | All |
| 2494 | D801 (#160) / Floor 2 Door W (#430) | D802 (#161) / Floor 1 Landing W (#437) | Down | — | All |
| 2495 | D801 (#160) / Floor 2 Door E (#432) | D802 (#161) / Floor 1 Landing E (#439) | Down | — | All |
| 2496 | D802 (#161) / Floor 1 Door W (#436) | D801 (#160) / Floor 2 Landing W (#431) | Up | — | All |
| 2497 | D802 (#161) / Floor 1 Door E (#438) | D801 (#160) / Floor 2 Landing E (#433) | Up | — | All |
| 2498 | D802 (#161) / Floor 3 Door W (#440) | D803 (#162) / Floor 2 Landing W (#448) | Down | — | All |
| 2499 | D802 (#161) / Floor 3 Door E (#442) | D803 (#162) / Floor 2 Landing E (#446) | Down | — | All |
| 2500 | D803 (#162) / Floor 2 Door E (#445) | D802 (#161) / Floor 3 Landing E (#443) | Up | — | All |
| 2501 | D803 (#162) / Floor 2 Door W (#447) | D802 (#161) / Floor 3 Landing W (#441) | Up | — | All |
| 2502 | D803 (#162) / Floor 4 Door (#449) | D804 (#163) / Floor 3 Landing (#454) | Down | — | All |
| 2503 | D804 (#163) / Floor 3 Door (#453) | D803 (#162) / Floor 4 Landing (#450) | Up | — | All |
| 2504 | D804 (#163) / Floor 5 Door (#455) | D805 (#164) / Landing (#460) | Down | — | All |
| 2505 | 0 (#1) / Ant Cave Entrance (#4) | D401 (#142) / Entrance Landing (#466) | Cave | — | All |
| 2506 | D401 (#142) / Entrance Door (#465) | 0 (#1) / Ant Cave Landing (#5) | Exit | — | All |
| 2507 | D401 (#142) / Floor 2 Door (#467) | D402 (#143) / Floor 1 Landing (#471) | Down | — | All |
| 2508 | D402 (#143) / Floor 1 Door (#470) | D401 (#142) / Floor 2 Landing (#468) | Up | — | All |
| 2509 | D402 (#143) / Floor 3 Door W (#472) | D403 (#144) / Floor 2 Landing W (#480) | Down | — | All |
| 2510 | D402 (#143) / Floor 3 Door E (#474) | D403 (#144) / Floor 2 Landing E (#482) | Down | — | All |
| 2511 | D403 (#144) / Floor 2 Door W (#479) | D402 (#143) / Floor 3 Landing W (#473) | Up | — | All |
| 2512 | D403 (#144) / Floor 2 Door E (#481) | D402 (#143) / Floor 3 Landing E (#475) | Up | — | All |
| 2513 | D403 (#144) / Floor 4 Door W (#483) | D404 (#145) / Floor 3 Landing W (#492) | Down | — | All |
| 2514 | D403 (#144) / Floor 4 Door E (#485) | D404 (#145) / Floor 3 Landing E (#494) | Down | — | All |
| 2515 | D404 (#145) / Floor 3 Door W (#491) | D403 (#144) / Floor 4 Landing W (#484) | Up | — | All |
| 2516 | D404 (#145) / Floor 3 Door E (#493) | D403 (#144) / Floor 4 Landing E (#486) | Up | — | All |
| 2517 | 0 (#1) / Bichon Caves Entrance (#8) | D101 (#26) / Entrance Landing (#92) | Cave | — | All |
| 2518 | D101 (#26) / Entrance Door (#91) | 0 (#1) / Bichon Caves Landing (#9) | Exit | — | All |
| 2519 | D101 (#26) / Top Right Door (#93) | D103 (#32) / Floor 1 Landing (#380) | Down | — | All |
| 2520 | D101 (#26) / Left Door  (#97) | D102 (#31) / Floor 1 Landing (#371) | Down | — | All |
| 2521 | D102 (#31) / Floor 1 Door (#370) | D101 (#26) / Left Landing (#98) | Up | — | All |
| 2522 | D102 (#31) / Floor 3 Door W (#372) | D103 (#32) / Floor 2 Landing W (#382) | Down | — | All |
| 2523 | D102 (#31) / Floor 3 Door E (#374) | D103 (#32) / Floor 2 Landing E (#384) | Down | — | All |
| 2524 | D103 (#32) / Floor 1 Door (#379) | D101 (#26) / Top Right Door Landing (#94) | Up | — | All |
| 2525 | D103 (#32) / Floor 2 Door W (#381) | D102 (#31) / Floor 3 Landing W (#373) | Up | — | All |
| 2526 | D103 (#32) / Floor 2 Door E (#383) | D102 (#31) / Floor 3 Landing E (#375) | Up | — | All |
| 2527 | 0 (#1) / Deserted Mines Entrance (#10) | D201 (#136) / Entrance Landing (#500) | Cave | — | All |
| 2528 | D201 (#136) / Entrance Door (#499) | 0 (#1) / Deserted Mines Landing (#11) | Exit | — | All |
| 2529 | D201 (#136) / Floor 2 Door (#501) | D202 (#137) / Floor 1 Landing (#509) | Down | — | All |
| 2530 | D202 (#137) / Floor 1 Door (#508) | D201 (#136) / Floor 2 Landing (#502) | Up | — | All |
| 2531 | D202 (#137) / Floor 3 Door (#510) | D203 (#138) / Landing (#517) | Down | — | All |
| 2532 | D203 (#138) / Door (#516) | D202 (#137) / Floor 3 Landing (#511) | Up | — | All |
| 2533 | 0 (#1) / Sabuk Wall Entrance (#14) | 3 (#7) / Bichon Town Landing (#144) | Province | — | All |
| 2534 | 3 (#7) / Bichon Town Door (#143) | 0 (#1) / Sabuk Wall Landing (#15) | Province | — | All |
| 2535 | 0 (#1) / Phantom Forest Entrance (#12) | D001 (#12) / Bichon Town Landing (#222) | Province | — | All |
| 2536 | D001 (#12) / Bichon Town Door (#221) | 0 (#1) / Phantom Forest Landing (#13) | Province | — | All |
| 2537 | 1 (#5) / Cave Door (#45) | D111 (#39) / Entrance Landing (#521) | Cave | — | All |
| 2538 | D111 (#39) / Entrance Door (#520) | 1 (#5) / Cave Landing (#46) | Exit | — | All |
| 2539 | D111 (#39) / Floor 2 Door (#522) | D112 (#40) / Floor 1 Landing (#530) | Down | — | All |
| 2540 | D111 (#39) / Floor 3 Door (#524) | D113 (#41) / Floor 1 Landing (#537) | Down | — | All |
| 2541 | D112 (#40) / Floor 3 Door W (#531) | D113 (#41) / Floor 2 Landing W (#539) | Down | — | All |
| 2542 | D112 (#40) / Floor 3 Door E (#533) | D113 (#41) / Floor 2 Landing E (#541) | Down | — | All |
| 2543 | D113 (#41) / Floor 1 Door (#536) | D111 (#39) / Floor 3 Landing (#525) | Up | — | All |
| 2544 | D113 (#41) / Floor 2 Door W (#538) | D112 (#40) / Floor 3 Landing W (#532) | Up | — | All |
| 2545 | D113 (#41) / Floor 2 Door E (#540) | D112 (#40) / Floor 3 Landing E (#534) | Up | — | All |
| 2546 | 1 (#5) / Paradise Forst Door (#47) | D003 (#14) / Lost Paradise Landing (#251) | Province | — | All |
| 2547 | D003 (#14) / Lost Paradise Door (#250) | 1 (#5) / Paradise Forst Landing (#48) | Province | — | All |
| 2548 | 1 (#5) / Stone Cave Door (#51) | D701 (#155) / Entrance Landing (#544) | Cave | — | All |
| 2549 | D701 (#155) / Entrance Door (#543) | 1 (#5) / Stone Cave Landing (#52) | Exit | — | All |
| 2550 | D701 (#155) / Floor 2 Door (#545) | D702 (#156) / Floor 1 Landing (#549) | Down | — | All |
| 2551 | D702 (#156) / Floor 1 Door (#548) | D701 (#155) / Floor 2 Landing (#546) | Up | — | All |
| 2552 | D702 (#156) / Floor 3 Door (#550) | D703 (#157) / Floor 2 Landing (#554) | Down | — | All |
| 2553 | D703 (#157) / Floor 2 Door (#553) | D702 (#156) / Floor 3 Landing (#551) | Up | — | All |
| 2554 | D703 (#157) / Floor 4 Door (#555) | D704 (#158) / Floor 3 Landing (#559) | Down | — | All |
| 2555 | D704 (#158) / Floor 3 Door (#558) | D703 (#157) / Floor 4 Landing (#556) | Up | — | All |
| 2556 | D704 (#158) / Floor 5 Door (#560) | D705 (#159) / Landing (#565) | Down | — | All |
| 2557 | 1 (#5) / Mud Wall Door (#55) | 5 (#9) / Lost Paradise Landing (#205) | Province | — | All |
| 2558 | 5 (#9) / Lost Paradise Door (#204) | 1 (#5) / Mud Wall Landing (#56) | Province | — | All |
| 2559 | 1 (#5) / Desert Door (#57) | D002 (#13) / Lost Paradise Landing (#237) | Province | — | All |
| 2560 | D002 (#13) / Lost Paradise Door (#236) | 1 (#5) / Desert Landing (#58) | Province | — | All |
| 2561 | 1 (#5) / Uma Door (#59) | D501 (#146) / Entrance Landing (#568) | Cave | — | All |
| 2562 | D501 (#146) / Entrance Door (#567) | 1 (#5) / Uma Landing  (#60) | Exit | — | All |
| 2563 | D501 (#146) / Floor 2 Door (#569) | D502 (#147) / Floor 1 Landing (#576) | Down | — | All |
| 2564 | D502 (#147) / Floor 1 Door (#575) | D501 (#146) / Floor 2 Landing (#570) | Up | — | All |
| 2565 | D502 (#147) / Floor 3 Door (#577) | D503 (#148) / Floor 2 Landing (#584) | Down | — | All |
| 2566 | D503 (#148) / Floor 2 Door (#583) | D502 (#147) / Floor 3 Landing  (#578) | Up | — | All |
| 2567 | D503 (#148) / Floor 4 Door (#585) | D504 (#149) / Landing (#591) | Down | — | All |
| 2568 | 3 (#7) / Zuma Temple Door (#141) | D1101 (#33) / Sabuk Landing (#389) | Cave | — | All |
| 2569 | D1101 (#33) / Sabuk Door (#388) | 3 (#7) / Zuma Temple Landing (#142) | Exit | — | All |
| 2571 | 3 (#7) / Phantom Forest Door (#147) | D001 (#12) / Sabuk Wall Landing (#595) | Province | — | All |
| 2572 | D001 (#12) / Sabuk Wall Door (#594) | 3 (#7) / Phantom Forest Landing (#148) | Province | — | All |
| 2573 | 3 (#7) / Banya Temple Door (#149) | D1001 (#16) / Sabuk Wall Landing (#263) | Cave | — | All |
| 2574 | D1001 (#16) / Sabuk Wall Door (#262) | 3 (#7) / Banya Temple Landing (#150) | Exit | — | All |
| 2575 | 3 (#7) / Banya Village Door (#151) | 2 (#6) / Sabuk Wall Landing (#120) | Province | — | All |
| 2577 | 3 (#7) / Red Moon Door (#145) | D901 (#165) / Sabuk Landing (#598) | Cave | — | All |
| 2578 | D901 (#165) / Sabuk Door (#597) | 3 (#7) / Red Moon Landing (#146) | Exit | — | All |
| 2579 | D1101 (#33) / Phantom Forest Door (#390) | D001 (#12) / Zuma Temple Landing (#220) | Exit | — | All |
| 2580 | D001 (#12) / Zuma Temple Door (#219) | D1101 (#33) / Phantom Forest Landing (#391) | Cave | — | All |
| 2581 | D1101 (#33) / Floor 2 Door (#399) | D1102 (#34) / Floor 1 Landing (#396) | Down | — | All |
| 2582 | D1102 (#34) / Floor 1 Door (#395) | D1101 (#33) / Floor 2 Landing (#400) | Up | — | All |
| 2583 | D1102 (#34) / Floor 3 Door (#397) | D1103 (#35) / Floor 2 Landing (#405) | Down | — | All |
| 2584 | D1103 (#35) / Floor 2 Door (#404) | D1102 (#34) / Floor 3 Landing (#398) | Up | — | All |
| 2585 | D1103 (#35) / Floor 4 Door (#402) | D1104 (#36) / Floor 3 Landing (#409) | Down | — | All |
| 2586 | D1104 (#36) / Floor 3 Door (#408) | D1103 (#35) / Floor 4 Landing (#403) | Up | — | All |
| 2587 | D1104 (#36) / Floor 5 Door  (#410) | D1105 (#37) / Floor 4 Landing (#415) | Down | — | All |
| 2588 | D1105 (#37) / Floor 4 Door (#414) | D1104 (#36) / Floor 5 Landing (#411) | Up | — | All |
| 2589 | D1105 (#37) / Floor 6 Door (#416) | D1106 (#38) / Landing (#425) | Down | — | All |
| 2591 | D901 (#165) / Phantom Forest Door (#599) | D001 (#12) / Red Moon Landing (#224) | Exit | — | All |
| 2592 | D001 (#12) / Red Moon Door (#223) | D901 (#165) / Phantom Forest Landing (#600) | Cave | — | All |
| 2593 | D901 (#165) / Floor 2 Door W (#601) | D902 (#166) / Floor 1 Landing W (#607) | Down | — | All |
| 2594 | D901 (#165) / Floor 2 Door E (#603) | D902 (#166) / Floor 1 Landing E (#609) | Down | — | All |
| 2595 | D902 (#166) / Floor 1 Door W (#606) | D901 (#165) / Floor 2 Landing W (#602) | Up | — | All |
| 2596 | D902 (#166) / Floor 1 Door E (#608) | D901 (#165) / Floor 2 Landing E (#604) | Up | — | All |
| 2597 | D902 (#166) / Floor 3 Door W (#610) | D903 (#167) / Floor 2 Landing W (#616) | Down | — | All |
| 2598 | D902 (#166) / Floor 3 Door E (#612) | D903 (#167) / Floor 2 Landing E (#618) | Down | — | All |
| 2599 | D903 (#167) / Floor 2 Door W (#615) | D902 (#166) / Floor 3  Landing W (#611) | Up | — | All |
| 2600 | D903 (#167) / Floor 2 Door E (#617) | D902 (#166) / Floor 3 Landing E (#613) | Up | — | All |
| 2601 | D903 (#167) / Floor 4 Door (#619) | D904 (#168) / Floor 3 Landing (#623) | Down | — | All |
| 2602 | D904 (#168) / Floor 3 Door (#622) | D903 (#167) / Floor 4 Landing (#620) | Up | — | All |
| 2603 | D904 (#168) / Floor 5 Door (#624) | D905 (#559) / Landing (#630) | Down | — | All |
| 2605 | D001 (#12) / Banya Village Door (#225) | 2 (#6) / Phantom Forest Landing (#122) | Province | — | All |
| 2606 | 2 (#6) / Phantom Forest Door (#121) | D001 (#12) / Banya Village Landing (#226) | Province | — | All |
| 2607 | D001 (#12) / Banya Temple Door (#227) | D1001 (#16) / Phantom Forest Landing (#265) | Cave | — | All |
| 2608 | D1001 (#16) / Phantom Forst Door (#264) | D001 (#12) / Banya Temple Landing (#228) | Exit | — | All |
| 2609 | D1001 (#16) / Floor 2 Door (#266) | D1002 (#17) / Floor 1 Landing (#274) | Down | — | All |
| 2610 | D1002 (#17) / Floor 1 Door (#273) | D1001 (#16) / Floor 2 Landing (#267) | Up | — | All |
| 2611 | D1002 (#17) / Floor 3 Door - E (#275) | D10032 (#19) / Floor 2 Landiing (#291) | Down | — | All |
| 2612 | D1002 (#17) / Floor 3 Door - W (#277) | D10031 (#18) / Landing (#287) | Down | — | All |
| 2613 | D10031 (#18) / Door (#286) | D1002 (#17) / Floor 3 Landing - W  (#278) | Up | — | All |
| 2614 | D10032 (#19) / Floor 2 Door (#290) | D1002 (#17) / Floor 3 Landing - E (#276) | Up | — | All |
| 2615 | D10032 (#19) / Floor 4 Door (#292) | D1004 (#20) / Floor 3 Landing - E (#296) | Down | — | All |
| 2616 | D1004 (#20) / Floor 3 Door - E (#295) | D10032 (#19) / Floor 4 Landing (#293) | Up | — | All |
| 2617 | D1004 (#20) / Floor 5 Door (#297) | D1005 (#21) / Landing (#304) | Down | — | All |
| 2618 | D1005 (#21) / Door (#303) | D1004 (#20) / Floor 5 Landning (#298) | Down | — | All |
| 2619 | D1006 (#22) / Door (#306) | D1007 (#23) / Hall Landing (#310) | Down | — | All |
| 2620 | D1007 (#23) / Hall Door (#309) | D1006 (#22) / Landing (#307) | Up | — | All |
| 2621 | D1007 (#23) / Floor 7 Door (#311) | D1008 (#24) / Floor 6 Landing (#321) | Down | — | All |
| 2622 | D1008 (#24) / Floor 6 Door (#320) | D1007 (#23) / Floor 7 Landing (#312) | Up | — | All |
| 2623 | D1008 (#24) / Floor 8 Door (#322) | D1009 (#25) / Floor 7 Landing (#332) | Down | — | All |
| 2624 | D1009 (#25) / Floor 7 Door (#331) | D1008 (#24) / Floor 8 Landing (#323) | Up | — | All |
| 2625 | D1009 (#25) / Floor 9 Door E (#333) | D10102 (#28) / Floor 8 Landing (#352) | Down | — | All |
| 2626 | D1009 (#25) / Floor 9 Door W (#335) | D10101 (#27) / Floor 8 Landing (#345) | Down | — | All |
| 2627 | D1009 (#25) / Floor 10 Door (#337) | D1011 (#29) / Floor 8 Landing (#359) | Down | — | All |
| 2628 | D10102 (#28) / Floor 8 Door (#351) | D1009 (#25) / Floor 9 Landing E (#334) | Up | — | All |
| 2629 | D10102 (#28) / Floor 10 Door (#353) | D1011 (#29) / Floor 9 Landing E (#361) | Down | — | All |
| 2630 | D10101 (#27) / Floor 8 Door (#344) | D1009 (#25) / Floor 9 Landing W (#336) | Up | — | All |
| 2631 | D10101 (#27) / Floor 10 Door (#346) | D1011 (#29) / Floor 9 Landing W (#363) | Down | — | All |
| 2632 | D1011 (#29) / Floor 8 Door (#358) | D1009 (#25) / Floor 10 Landing (#338) | Up | — | All |
| 2633 | D1011 (#29) / Floor 9 Door E (#360) | D10102 (#28) / Floor 10 Landing (#354) | Up | — | All |
| 2634 | D1011 (#29) / Floor 9 Door W (#362) | D10101 (#27) / Floor 10 Landing (#347) | Up | — | All |
| 2635 | D1011 (#29) / Floor 11 Door (#364) | D1012 (#30) / Landing (#633) | Down | — | All |
| 2636 | 2 (#6) / Sabuk Wall Door (#119) | 3 (#7) / Banya Village Landing (#152) | Province | — | All |
| 2637 | 2 (#6) / Flea Cave Door (#117) | D301 (#139) / Entrance Landing (#647) | Cave | — | All |
| 2638 | D301 (#139) / Entrance Door (#646) | 2 (#6) / Flea Cave Landing (#118) | Exit | — | All |
| 2639 | 2 (#6) / Cave Door (#123) | D121 (#59) / Entrance Landing (#665) | Cave | — | All |
| 2640 | D121 (#59) / Entance Door (#664) | 2 (#6) / Cave Landing (#124) | Exit | — | All |
| 2641 | 2 (#6) / Banya South Door (#125) | D004 (#15) / Banya Village Landing (#255) | Province | — | All |
| 2642 | D004 (#15) / Banya Village Door (#254) | 2 (#6) / Bany South Landing (#126) | Province | — | All |
| 2645 | 2 (#6) / South Way Door (#129) | E12 (#225) / Banya Village Landing (#644) | Province | — | All |
| 2646 | E12 (#225) / Banya Village Door (#643) | 2 (#6) / South Way Landing (#130) | Province | — | All |
| 2647 | D121 (#59) / Floor 3 Door (#666) | D123 (#61) / Floor 1 Landing (#679) | Down | — | All |
| 2648 | D121 (#59) / Floor 2 Door (#668) | D122 (#60) / Floor 1 Landing (#672) | Down | — | All |
| 2649 | D122 (#60) / Floor 1 Door (#671) | D121 (#59) / Floor 2 Landing (#669) | Up | — | All |
| 2650 | D122 (#60) / Floor 3 Door W (#673) | D123 (#61) / Floor 2 Landing W (#681) | Down | — | All |
| 2651 | D122 (#60) / Floor 3 Door E (#675) | D123 (#61) / Floor 2 Landing E (#683) | Down | — | All |
| 2652 | D123 (#61) / Floor 1 Door (#678) | D121 (#59) / Floor 3 Landing (#667) | Up | — | All |
| 2653 | D123 (#61) / Floor 2 Door W (#680) | D122 (#60) / Floor 3 Landing W (#674) | Up | — | All |
| 2654 | D123 (#61) / Floor 2 Door E (#682) | D122 (#60) / Floor 3 Landing E (#676) | Up | — | All |
| 2655 | 2 (#6) / Stone Cave Door (#127) | D601 (#150) / Entrance Landing (#685) | Cave | — | All |
| 2656 | D601 (#150) / Entrance Door (#684) | 2 (#6) / Stone Cave Landing (#128) | Exit | — | All |
| 2657 | D601 (#150) / Floor 2 Door (#686) | D602 (#151) / Floor 1 Landing (#690) | Down | — | All |
| 2658 | D602 (#151) / Floor 1 Door (#689) | D601 (#150) / Floor 2 Landing (#687) | Up | — | All |
| 2659 | D602 (#151) / Floor 3 Door (#691) | D603 (#152) / Floor 2 Landing (#696) | Down | — | All |
| 2660 | D603 (#152) / Floor 2 Door (#695) | D602 (#151) / Floor 3 Landing (#692) | Up | — | All |
| 2661 | D603 (#152) / Floor 4 Door (#697) | D604 (#153) / Floor 3 Landing (#702) | Down | — | All |
| 2662 | D604 (#153) / Floor 3 Door (#701) | D603 (#152) / Floor 4 Landing (#698) | Up | — | All |
| 2663 | D604 (#153) / Floor 5 Door (#703) | D605 (#154) / Floor 4 Landing (#707) | Down | — | All |
| 2664 | D605 (#154) / Floor 4 Door (#706) | D604 (#153) / Floor 5 Landing (#704) | Up | — | All |
| 2665 | E12 (#225) / Left Door (#641) | E11 (#224) / Right Landing (#637) | Province | — | All |
| 2666 | E11 (#224) / Right Door (#636) | E12 (#225) / Left Landing (#642) | Province | — | All |
| 2667 | E11 (#224) / Numa Village Door (#638) | 4 (#8) / South Way Landing (#177) | Province | — | All |
| 2668 | 4 (#8) / South Way Door (#176) | E11 (#224) / Numa Village Landing (#639) | Province | — | All |
| 2669 | 4 (#8) / Desert Door (#172) | D002 (#13) / Numa Village Landing (#243) | Province | — | All |
| 2670 | 4 (#8) / Mud Wall Door (#174) | 5 (#9) / Numa Village Landing (#211) | Province | — | All |
| 2671 | 5 (#9) / Desert Door (#202) | D002 (#13) / Mud Wall Landing (#239) | Province | — | All |
| 2672 | 5 (#9) / Numa Village Door (#210) | 4 (#8) / Mud Wall Landing (#175) | Province | — | All |
| 2673 | D002 (#13) / Mud Wall Door (#238) | 5 (#9) / Desert Landing (#203) | Province | — | All |
| 2674 | D002 (#13) / Numa Village Door (#242) | 4 (#8) / Desert Landing (#173) | Province | — | All |
| 2675 | D1401 (#68) / Boat Door (#713) | D1402 (#69) / Boat Landing (#717) | Cave | — | All |
| 2676 | D1402 (#69) / Floor 3 Door (#718) | D1403 (#70) / Floor 4 Landing (#723) | Down | — | All |
| 2677 | D1403 (#70) / Floor 4 Door (#722) | D1402 (#69) / Floor 3 Landing (#719) | Up | — | All |
| 2678 | D1403 (#70) / Floor 2 Door  (#724) | D1404 (#71) / Floor 3 Landing (#729) | Down | — | All |
| 2679 | D1404 (#71) / Floor 3 Door (#728) | D1403 (#70) / Floor 2 Landing (#725) | Up | — | All |
| 2680 | D1404 (#71) / Floor 1 Door (#730) | D1405 (#72) / Floor 2 Landing (#735) | Down | — | All |
| 2681 | D1405 (#72) / Floor 2 Door (#734) | D1404 (#71) / Floor 1 Landing (#731) | Up | — | All |
| 2682 | D1405 (#72) / Flight Deck Door (#736) | D1406 (#73) / Landing (#743) | Down | Yun Wine (#623) | All |
| 2683 | D002 (#13) / West Deset Door (#244) | D2001 (#125) / Entrance Landing (#748) | Cave | — | All |
| 2684 | D2001 (#125) / Entrance Door (#747) | D002 (#13) / West Desert Landing (#245) | Exit | — | All |
| 2685 | D2001 (#125) / Floor 2 Door (#749) | D20011 (#126) / Floor 1 Landing (#753) | Down | — | All |
| 2686 | D20011 (#126) / Floor 1 Door (#752) | D2001 (#125) / Floor 2 Landing (#750) | Up | — | All |
| 2687 | D20011 (#126) / Floor 3 Door (#754) | D20012 (#127) / Floor 2 Landing (#758) | Down | — | All |
| 2688 | D20012 (#127) / Floor 2 Door (#757) | D20011 (#126) / Floor 3 Landing (#755) | Up | — | All |
| 2689 | D20012 (#127) / City Door (#759) | D2002 (#128) / Floor 3 Landing (#763) | Cave | — | All |
| 2690 | D2002 (#128) / Floor 3 Door (#762) | D20012 (#127) / City Landing (#760) | Exit | — | All |
| 2691 | D2002 (#128) / City 2 Door (#764) | D20021 (#129) / City 1 Landing (#769) | Down | — | All |
| 2692 | D2002 (#128) / Trap (#766) | D2002 (#128) / Whole Map (#761) | None | — | All |
| 2693 | D20021 (#129) / City 1 Door (#768) | D2002 (#128) / City 2 Landing (#765) | Up | — | All |
| 2694 | D20021 (#129) / City 3 Door (#770) | D20022 (#130) / City 2 Landing (#782) | Down | — | All |
| 2695 | D20021 (#129) / Mine 1 Door (#772) | D2003 (#132) / City 2 Landing (#795) | Cave | — | All |
| 2696 | D20021 (#129) / Traps (#774) | D20021 (#129) / Whole Map (#767) | None | — | All |
| 2697 | D20022 (#130) / City 2 Door (#781) | D20021 (#129) / City 3 Landing (#771) | Up | — | All |
| 2698 | D20022 (#130) / City 4 Door (#783) | D20023 (#131) / City 3 Landing (#787) | Down | — | All |
| 2699 | D20023 (#131) / City 3 Door (#786) | D20022 (#130) / City 4 Landing (#784) | Up | — | All |
| 2700 | D20023 (#131) / Royal Room Door (#788) | D2004 (#135) / Landing (#813) | Down | — | All |
| 2701 | D20023 (#131) / Traps (#790) | D20023 (#131) / Whole Map (#785) | None | — | All |
| 2702 | D2003 (#132) / City 2 Door (#794) | D20021 (#129) / Mine 1 Landing (#773) | Exit | — | All |
| 2703 | D2003 (#132) / Mine 2 Door (#796) | D20031 (#133) / Mine 1 Landing (#800) | Down | — | All |
| 2704 | D20031 (#133) / Mine 1 Door (#799) | D2003 (#132) / Mine 2 Landing (#797) | Up | — | All |
| 2705 | D20031 (#133) / Top Right Door (#801) | D20031 (#133) / Bottom Left Landing (#805) | Up | — | All |
| 2706 | D20031 (#133) / Bottom Left Door (#804) | D20031 (#133) / Top Right Landing (#802) | Up | — | All |
| 2707 | D20031 (#133) / Mine 3 Door (#807) | D20032 (#134) / Mine 2 Landing (#810) | Down | — | All |
| 2708 | D20032 (#134) / Mine 2 Door (#809) | D20031 (#133) / Mine 3 Landing (#808) | Up | — | All |
| 2709 | D003 (#14) / Homeland Door (#246) | D005 (#242) / Lost Paradise Forest Landing (#817) | Province | — | All |
| 2710 | D005 (#242) / Lost Paradise Forest Door (#816) | D003 (#14) / HomeLand Landing (#247) | Province | — | All |
| 2711 | D005 (#242) / Frost Village Door (#818) | 8 (#241) / Homeland Landing (#838) | Province | — | All |
| 2712 | 8 (#241) / Homeland Door (#837) | D005 (#242) / Frost Village Landing (#819) | Province | — | All |
| 2713 | 8 (#241) / Frost Dungeon Door (#839) | D2101 (#243) / Entrance Landing (#848) | Cave | — | All |
| 2714 | D2101 (#243) / Entrance Door (#847) | 8 (#241) / Frost Dungeon Landing (#840) | Exit | — | All |
| 2715 | D2101 (#243) / Floor 2 Door (#849) | D2102 (#244) / Floor 1 Landing (#853) | Down | — | All |
| 2716 | D2102 (#244) / Floor 1 Door (#852) | D2101 (#243) / Floor 2 Landing (#850) | Up | — | All |
| 2717 | D2102 (#244) / Floor 3 Door (#854) | D2103 (#245) / Floor 2 Landing (#858) | Down | — | All |
| 2718 | D2103 (#245) / Floor 2 Door (#857) | D2102 (#244) / Floor 3 Landing (#855) | Up | — | All |
| 2719 | D2103 (#245) / Floor 4 Door (#859) | D2104 (#246) / Floor 3 Landing (#863) | Down | — | All |
| 2720 | D2104 (#246) / Floor 3 Door (#862) | D2103 (#245) / Floor 4 Landing (#860) | Up | — | All |
| 2721 | D2104 (#246) / Floor 5 Door (#864) | D21051 (#247) / Top Landing (#869) | Down | — | All |
| 2722 | D21051 (#247) / Top Door (#868) | D21051 (#247) / Bottom Landing (#873) | None | — | All |
| 2723 | D21051 (#247) / Right Door (#870) | D21051 (#247) / Left Landing (#875) | None | — | All |
| 2724 | D21051 (#247) / Bottom Door (#872) | D21052 (#248) / Top Landing (#879) | None | — | All |
| 2725 | D21051 (#247) / Left Door (#874) | D21051 (#247) / Right Landing (#871) | None | — | All |
| 2726 | D21052 (#248) / Top Door (#878) | D21051 (#247) / Bottom Landing (#873) | None | — | All |
| 2727 | D21052 (#248) / Right Door (#880) | D21053 (#249) / Left Landing (#894) | None | — | All |
| 2728 | D21052 (#248) / Bottom Door (#882) | D21051 (#247) / Top Landing (#869) | None | — | All |
| 2729 | D21052 (#248) / Left Door (#884) | D21051 (#247) / Right Landing (#871) | None | — | All |
| 2730 | D21053 (#249) / Top Door (#887) | D21051 (#247) / Bottom Landing (#873) | None | — | All |
| 2731 | D21053 (#249) / Right Door (#889) | D21051 (#247) / Left Landing (#875) | None | — | All |
| 2732 | D21053 (#249) / Bottom Door (#891) | D21054 (#250) / Top Landing (#897) | None | — | All |
| 2733 | D21053 (#249) / Left Door (#893) | D21051 (#247) / Right Landing (#871) | None | — | All |
| 2734 | D21054 (#250) / Top Door (#896) | D21051 (#247) / Bottom Landing (#873) | None | — | All |
| 2735 | D21054 (#250) / Right Door (#898) | D21051 (#247) / Left Landing (#875) | None | — | All |
| 2736 | D21054 (#250) / Bottom Door (#900) | D21051 (#247) / Top Landing (#869) | None | — | All |
| 2737 | D21054 (#250) / Left Door (#902) | D21055 (#254) / Right Landing (#913) | None | — | All |
| 2738 | D21055 (#254) / Top Door (#910) | D21056 (#255) / Bottom Landing (#924) | None | — | All |
| 2739 | D21055 (#254) / Right Door (#912) | D21051 (#247) / Left Landing (#875) | None | — | All |
| 2740 | D21055 (#254) / Bottom Door (#914) | D21051 (#247) / Top Landing (#869) | None | — | All |
| 2741 | D21055 (#254) / Left Door (#916) | D21051 (#247) / Right Landing (#871) | None | — | All |
| 2742 | D21056 (#255) / Top Door (#919) | D21051 (#247) / Bottom Landing (#873) | None | — | All |
| 2743 | D21056 (#255) / Right Door (#921) | D2106 (#251) / Landing (#906) | None | — | All |
| 2744 | D21056 (#255) / Bottom Door (#923) | D21051 (#247) / Top Landing (#869) | None | — | All |
| 2745 | D21056 (#255) / Left Door (#925) | D21051 (#247) / Right Landing (#871) | None | — | All |
| 2746 | 4 (#8) / Numa Door N (#178) | D1501 (#74) / Entrance Top Landing (#931) | Cave | — | All |
| 2747 | 4 (#8) / Numa Door E (#180) | D1501 (#74) / Entrance Right Landing (#933) | Cave | — | All |
| 2748 | 4 (#8) / Numa Door S (#182) | D1501 (#74) / Entrance Bottom Landing (#935) | Cave | — | All |
| 2749 | 4 (#8) / Numa Door W (#184) | D1501 (#74) / Entrance Left Landing (#937) | Cave | — | All |
| 2750 | D1501 (#74) / Entrance Top Door (#930) | 4 (#8) / Numa Landing N (#179) | Exit | — | All |
| 2751 | D1501 (#74) / Entrance Right Door (#932) | 4 (#8) / Numa Landing E (#181) | Exit | — | All |
| 2752 | D1501 (#74) / Entrance Bottom Door (#934) | 4 (#8) / Numa Landing S (#183) | Exit | — | All |
| 2753 | D1501 (#74) / Entrance Left Door (#936) | 4 (#8) / Numa Landing W (#185) | Exit | — | All |
| 2754 | D1501 (#74) / Floor 2 Door (#938) | D1502 (#75) / Floor 1 Landing (#942) | Down | — | All |
| 2755 | D1502 (#75) / Floor 1 Door (#941) | D1501 (#74) / Floor 2 Landing (#939) | Up | — | All |
| 2756 | D1502 (#75) / Floor 3 Top Door (#943) | D15032 (#77) / Top Landing (#959) | Down | — | All |
| 2757 | D1502 (#75) / Floor 3 Right Door (#945) | D15031 (#76) / Right Landing (#954) | Down | — | All |
| 2758 | D1502 (#75) / Floor 3 Bottom Door (#947) | D15034 (#79) / Bottom Landing (#969) | Down | — | All |
| 2759 | D1502 (#75) / Floor 3 Left Door (#949) | D15033 (#78) / Left Landing (#964) | Down | — | All |
| 2760 | D15032 (#77) / Top Floor 4 Door (#960) | D1504 (#80) / Floor 3 Top Landing (#1105) | Down | — | All |
| 2761 | D15031 (#76) / Right Floor 4 Door (#955) | D1504 (#80) / Floor 3 Right Landing (#1106) | Down | — | All |
| 2762 | D15034 (#79) / Bottom Floor 4 Door (#970) | D1504 (#80) / Floor 3 Bottom Landing (#1107) | Down | — | All |
| 2763 | D15033 (#78) / Left Floor 4 Door (#965) | D1504 (#80) / Floor 3 Left Landing (#1108) | Down | — | All |
| 2764 | D1504 (#80) / Floor 3 Top Door (#1101) | D15032 (#77) / Top Floor 4 Landing (#961) | Up | — | All |
| 2765 | D1504 (#80) / Floor 3 Right Door (#1102) | D15031 (#76) / Right Floor 4 Landing (#956) | Up | — | All |
| 2766 | D1504 (#80) / Floor 3 Bottom Door (#1103) | D15034 (#79) / Bottom Floor 4 Landing (#971) | Up | — | All |
| 2767 | D1504 (#80) / Floor 3 Left Door (#1104) | D15033 (#78) / Left Floor 4 Landing (#966) | Up | — | All |
| 2768 | D1504 (#80) / Fake Bottom Doors Top Area (#975) | D1504 (#80) / Top Landing Top Area (#978) | None | — | All |
| 2769 | D1504 (#80) / Fake Top Doors Top Area (#976) | D1504 (#80) / Bottom Landing Top Area (#977) | None | — | All |
| 2770 | D1504 (#80) / Fake Bottom Doors Left Area (#979) | D1504 (#80) / Top Landing Top Area (#978) | None | — | All |
| 2771 | D1504 (#80) / Fake Top Doors  Left Area (#980) | D1504 (#80) / Bottom Landing Top Area (#977) | None | — | All |
| 2772 | D1504 (#80) / Fake Bottom Doors Right Area (#983) | D1504 (#80) / Top Landing  Left Area (#982) | None | — | All |
| 2773 | D1504 (#80) / Fake Top Doors  Right Area (#984) | D1504 (#80) / Bottom Landing  Left Area (#981) | None | — | All |
| 2774 | D1504 (#80) / Real Bottom Door Top Aea (#987) | D1504 (#80) / Top Landing  Left Area (#982) | None | — | All |
| 2775 | D1504 (#80) / Real Top Door Top Area (#988) | D1504 (#80) / Bottom Landing  Left Area (#981) | None | — | All |
| 2776 | D1504 (#80) / Real Bottom Door Left Area (#989) | D1504 (#80) / Top Landing  Right Area (#986) | None | — | All |
| 2777 | D1504 (#80) / Real Top Door  Left Area (#990) | D1504 (#80) / Bottom Landing  Right Area (#985) | None | — | All |
| 2778 | D1504 (#80) / Real Bottom Door Right Area (#991) | D1505 (#81) / Row 1 Top Landing (#1012) | None | — | All |
| 2779 | D1504 (#80) / Real Top Door  Right Area (#992) | D1505 (#81) / Row 1 Bottom Landing (#1011) | None | — | All |
| 2780 | D1505 (#81) / Row 1 Fake Bottom Doors (#1009) | D1505 (#81) / Row 1 Top Landing (#1012) | None | — | All |
| 2781 | D1505 (#81) / Row 1 Fake Top Doors (#1010) | D1505 (#81) / Row 1 Bottom Landing (#1011) | None | — | All |
| 2782 | D1505 (#81) / Row 1 Real Bottom Door (#1071) | D1505 (#81) / Row 2 Top Landing (#1022) | None | — | All |
| 2783 | D1505 (#81) / Row 1 Real Top Door (#1072) | D1505 (#81) / Row 2 Bottom Landing (#1021) | None | — | All |
| 2784 | D1505 (#81) / Row 2 Fake Bottom Doors (#1019) | D1505 (#81) / Row 1 Top Landing (#1012) | None | — | All |
| 2785 | D1505 (#81) / Row 2 Fake Top Doors (#1020) | D1505 (#81) / Row 1 Bottom Landing (#1011) | None | — | All |
| 2786 | D1505 (#81) / Row 2 Real Bottom door (#1073) | D1505 (#81) / Row 3 Top Landing (#1026) | None | — | All |
| 2787 | D1505 (#81) / Row 2 Real Top door (#1074) | D1505 (#81) / Row 3 Bottom Landing (#1025) | None | — | All |

### #2482 · 0 (#1) / Palace Entrance (#19) / 0_000 (#2) / Entrance Landing (#35)

| 字段 | 值 |
|---|---|
| SourceRegion | 0 (#1) / Palace Entrance (#19) |
| DestinationRegion | 0_000 (#2) / Entrance Landing (#35) |
| Icon | Building |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2483 · 0_000 (#2) / Entrance Door (#34) / 0 (#1) / Palace Landing (#20)

| 字段 | 值 |
|---|---|
| SourceRegion | 0_000 (#2) / Entrance Door (#34) |
| DestinationRegion | 0 (#1) / Palace Landing (#20) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2484 · 0_000 (#2) / Left Door (#36) / 0_001 (#3) / Landing (#41)

| 字段 | 值 |
|---|---|
| SourceRegion | 0_000 (#2) / Left Door (#36) |
| DestinationRegion | 0_001 (#3) / Landing (#41) |
| Icon | Building |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2485 · 0_000 (#2) / Right Door (#38) / 0_002 (#4) / Landing (#43)

| 字段 | 值 |
|---|---|
| SourceRegion | 0_000 (#2) / Right Door (#38) |
| DestinationRegion | 0_002 (#4) / Landing (#43) |
| Icon | Building |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2486 · 0 (#1) / North Way Entrance (#16) / E02 (#223) / Bichon Town Landing (#87)

| 字段 | 值 |
|---|---|
| SourceRegion | 0 (#1) / North Way Entrance (#16) |
| DestinationRegion | E02 (#223) / Bichon Town Landing (#87) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2487 · E02 (#223) / Bichon Town Door (#85) / 0 (#1) / North Way Landing (#17)

| 字段 | 值 |
|---|---|
| SourceRegion | E02 (#223) / Bichon Town Door (#85) |
| DestinationRegion | 0 (#1) / North Way Landing (#17) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2488 · E01 (#222) / Lost Paradise Door (#82) / 1 (#5) / North Way Landing (#54)

| 字段 | 值 |
|---|---|
| SourceRegion | E01 (#222) / Lost Paradise Door (#82) |
| DestinationRegion | 1 (#5) / North Way Landing (#54) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2489 · 1 (#5) / North Way Door (#53) / E01 (#222) / Lost Paradise Landing (#86)

| 字段 | 值 |
|---|---|
| SourceRegion | 1 (#5) / North Way Door (#53) |
| DestinationRegion | E01 (#222) / Lost Paradise Landing (#86) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2490 · E02 (#223) / Left Door (#83) / E01 (#222) / Right Landing (#81)

| 字段 | 值 |
|---|---|
| SourceRegion | E02 (#223) / Left Door (#83) |
| DestinationRegion | E01 (#222) / Right Landing (#81) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2491 · E01 (#222) / Right Door (#80) / E02 (#223) / Left Landing (#84)

| 字段 | 值 |
|---|---|
| SourceRegion | E01 (#222) / Right Door (#80) |
| DestinationRegion | E02 (#223) / Left Landing (#84) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2492 · 0 (#1) / Bug Cave Entrance (#2) / D801 (#160) / Entrance Landing (#429)

| 字段 | 值 |
|---|---|
| SourceRegion | 0 (#1) / Bug Cave Entrance (#2) |
| DestinationRegion | D801 (#160) / Entrance Landing (#429) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2493 · D801 (#160) / Entrance Door (#428) / 0 (#1) / Bug Cave Landing (#3)

| 字段 | 值 |
|---|---|
| SourceRegion | D801 (#160) / Entrance Door (#428) |
| DestinationRegion | 0 (#1) / Bug Cave Landing (#3) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2494 · D801 (#160) / Floor 2 Door W (#430) / D802 (#161) / Floor 1 Landing W (#437)

| 字段 | 值 |
|---|---|
| SourceRegion | D801 (#160) / Floor 2 Door W (#430) |
| DestinationRegion | D802 (#161) / Floor 1 Landing W (#437) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2495 · D801 (#160) / Floor 2 Door E (#432) / D802 (#161) / Floor 1 Landing E (#439)

| 字段 | 值 |
|---|---|
| SourceRegion | D801 (#160) / Floor 2 Door E (#432) |
| DestinationRegion | D802 (#161) / Floor 1 Landing E (#439) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2496 · D802 (#161) / Floor 1 Door W (#436) / D801 (#160) / Floor 2 Landing W (#431)

| 字段 | 值 |
|---|---|
| SourceRegion | D802 (#161) / Floor 1 Door W (#436) |
| DestinationRegion | D801 (#160) / Floor 2 Landing W (#431) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2497 · D802 (#161) / Floor 1 Door E (#438) / D801 (#160) / Floor 2 Landing E (#433)

| 字段 | 值 |
|---|---|
| SourceRegion | D802 (#161) / Floor 1 Door E (#438) |
| DestinationRegion | D801 (#160) / Floor 2 Landing E (#433) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2498 · D802 (#161) / Floor 3 Door W (#440) / D803 (#162) / Floor 2 Landing W (#448)

| 字段 | 值 |
|---|---|
| SourceRegion | D802 (#161) / Floor 3 Door W (#440) |
| DestinationRegion | D803 (#162) / Floor 2 Landing W (#448) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2499 · D802 (#161) / Floor 3 Door E (#442) / D803 (#162) / Floor 2 Landing E (#446)

| 字段 | 值 |
|---|---|
| SourceRegion | D802 (#161) / Floor 3 Door E (#442) |
| DestinationRegion | D803 (#162) / Floor 2 Landing E (#446) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2500 · D803 (#162) / Floor 2 Door E (#445) / D802 (#161) / Floor 3 Landing E (#443)

| 字段 | 值 |
|---|---|
| SourceRegion | D803 (#162) / Floor 2 Door E (#445) |
| DestinationRegion | D802 (#161) / Floor 3 Landing E (#443) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2501 · D803 (#162) / Floor 2 Door W (#447) / D802 (#161) / Floor 3 Landing W (#441)

| 字段 | 值 |
|---|---|
| SourceRegion | D803 (#162) / Floor 2 Door W (#447) |
| DestinationRegion | D802 (#161) / Floor 3 Landing W (#441) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2502 · D803 (#162) / Floor 4 Door (#449) / D804 (#163) / Floor 3 Landing (#454)

| 字段 | 值 |
|---|---|
| SourceRegion | D803 (#162) / Floor 4 Door (#449) |
| DestinationRegion | D804 (#163) / Floor 3 Landing (#454) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2503 · D804 (#163) / Floor 3 Door (#453) / D803 (#162) / Floor 4 Landing (#450)

| 字段 | 值 |
|---|---|
| SourceRegion | D804 (#163) / Floor 3 Door (#453) |
| DestinationRegion | D803 (#162) / Floor 4 Landing (#450) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2504 · D804 (#163) / Floor 5 Door (#455) / D805 (#164) / Landing (#460)

| 字段 | 值 |
|---|---|
| SourceRegion | D804 (#163) / Floor 5 Door (#455) |
| DestinationRegion | D805 (#164) / Landing (#460) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2505 · 0 (#1) / Ant Cave Entrance (#4) / D401 (#142) / Entrance Landing (#466)

| 字段 | 值 |
|---|---|
| SourceRegion | 0 (#1) / Ant Cave Entrance (#4) |
| DestinationRegion | D401 (#142) / Entrance Landing (#466) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2506 · D401 (#142) / Entrance Door (#465) / 0 (#1) / Ant Cave Landing (#5)

| 字段 | 值 |
|---|---|
| SourceRegion | D401 (#142) / Entrance Door (#465) |
| DestinationRegion | 0 (#1) / Ant Cave Landing (#5) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2507 · D401 (#142) / Floor 2 Door (#467) / D402 (#143) / Floor 1 Landing (#471)

| 字段 | 值 |
|---|---|
| SourceRegion | D401 (#142) / Floor 2 Door (#467) |
| DestinationRegion | D402 (#143) / Floor 1 Landing (#471) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2508 · D402 (#143) / Floor 1 Door (#470) / D401 (#142) / Floor 2 Landing (#468)

| 字段 | 值 |
|---|---|
| SourceRegion | D402 (#143) / Floor 1 Door (#470) |
| DestinationRegion | D401 (#142) / Floor 2 Landing (#468) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2509 · D402 (#143) / Floor 3 Door W (#472) / D403 (#144) / Floor 2 Landing W (#480)

| 字段 | 值 |
|---|---|
| SourceRegion | D402 (#143) / Floor 3 Door W (#472) |
| DestinationRegion | D403 (#144) / Floor 2 Landing W (#480) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2510 · D402 (#143) / Floor 3 Door E (#474) / D403 (#144) / Floor 2 Landing E (#482)

| 字段 | 值 |
|---|---|
| SourceRegion | D402 (#143) / Floor 3 Door E (#474) |
| DestinationRegion | D403 (#144) / Floor 2 Landing E (#482) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2511 · D403 (#144) / Floor 2 Door W (#479) / D402 (#143) / Floor 3 Landing W (#473)

| 字段 | 值 |
|---|---|
| SourceRegion | D403 (#144) / Floor 2 Door W (#479) |
| DestinationRegion | D402 (#143) / Floor 3 Landing W (#473) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2512 · D403 (#144) / Floor 2 Door E (#481) / D402 (#143) / Floor 3 Landing E (#475)

| 字段 | 值 |
|---|---|
| SourceRegion | D403 (#144) / Floor 2 Door E (#481) |
| DestinationRegion | D402 (#143) / Floor 3 Landing E (#475) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2513 · D403 (#144) / Floor 4 Door W (#483) / D404 (#145) / Floor 3 Landing W (#492)

| 字段 | 值 |
|---|---|
| SourceRegion | D403 (#144) / Floor 4 Door W (#483) |
| DestinationRegion | D404 (#145) / Floor 3 Landing W (#492) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2514 · D403 (#144) / Floor 4 Door E (#485) / D404 (#145) / Floor 3 Landing E (#494)

| 字段 | 值 |
|---|---|
| SourceRegion | D403 (#144) / Floor 4 Door E (#485) |
| DestinationRegion | D404 (#145) / Floor 3 Landing E (#494) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2515 · D404 (#145) / Floor 3 Door W (#491) / D403 (#144) / Floor 4 Landing W (#484)

| 字段 | 值 |
|---|---|
| SourceRegion | D404 (#145) / Floor 3 Door W (#491) |
| DestinationRegion | D403 (#144) / Floor 4 Landing W (#484) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2516 · D404 (#145) / Floor 3 Door E (#493) / D403 (#144) / Floor 4 Landing E (#486)

| 字段 | 值 |
|---|---|
| SourceRegion | D404 (#145) / Floor 3 Door E (#493) |
| DestinationRegion | D403 (#144) / Floor 4 Landing E (#486) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2517 · 0 (#1) / Bichon Caves Entrance (#8) / D101 (#26) / Entrance Landing (#92)

| 字段 | 值 |
|---|---|
| SourceRegion | 0 (#1) / Bichon Caves Entrance (#8) |
| DestinationRegion | D101 (#26) / Entrance Landing (#92) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2518 · D101 (#26) / Entrance Door (#91) / 0 (#1) / Bichon Caves Landing (#9)

| 字段 | 值 |
|---|---|
| SourceRegion | D101 (#26) / Entrance Door (#91) |
| DestinationRegion | 0 (#1) / Bichon Caves Landing (#9) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2519 · D101 (#26) / Top Right Door (#93) / D103 (#32) / Floor 1 Landing (#380)

| 字段 | 值 |
|---|---|
| SourceRegion | D101 (#26) / Top Right Door (#93) |
| DestinationRegion | D103 (#32) / Floor 1 Landing (#380) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2520 · D101 (#26) / Left Door  (#97) / D102 (#31) / Floor 1 Landing (#371)

| 字段 | 值 |
|---|---|
| SourceRegion | D101 (#26) / Left Door  (#97) |
| DestinationRegion | D102 (#31) / Floor 1 Landing (#371) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2521 · D102 (#31) / Floor 1 Door (#370) / D101 (#26) / Left Landing (#98)

| 字段 | 值 |
|---|---|
| SourceRegion | D102 (#31) / Floor 1 Door (#370) |
| DestinationRegion | D101 (#26) / Left Landing (#98) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2522 · D102 (#31) / Floor 3 Door W (#372) / D103 (#32) / Floor 2 Landing W (#382)

| 字段 | 值 |
|---|---|
| SourceRegion | D102 (#31) / Floor 3 Door W (#372) |
| DestinationRegion | D103 (#32) / Floor 2 Landing W (#382) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2523 · D102 (#31) / Floor 3 Door E (#374) / D103 (#32) / Floor 2 Landing E (#384)

| 字段 | 值 |
|---|---|
| SourceRegion | D102 (#31) / Floor 3 Door E (#374) |
| DestinationRegion | D103 (#32) / Floor 2 Landing E (#384) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2524 · D103 (#32) / Floor 1 Door (#379) / D101 (#26) / Top Right Door Landing (#94)

| 字段 | 值 |
|---|---|
| SourceRegion | D103 (#32) / Floor 1 Door (#379) |
| DestinationRegion | D101 (#26) / Top Right Door Landing (#94) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2525 · D103 (#32) / Floor 2 Door W (#381) / D102 (#31) / Floor 3 Landing W (#373)

| 字段 | 值 |
|---|---|
| SourceRegion | D103 (#32) / Floor 2 Door W (#381) |
| DestinationRegion | D102 (#31) / Floor 3 Landing W (#373) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2526 · D103 (#32) / Floor 2 Door E (#383) / D102 (#31) / Floor 3 Landing E (#375)

| 字段 | 值 |
|---|---|
| SourceRegion | D103 (#32) / Floor 2 Door E (#383) |
| DestinationRegion | D102 (#31) / Floor 3 Landing E (#375) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2527 · 0 (#1) / Deserted Mines Entrance (#10) / D201 (#136) / Entrance Landing (#500)

| 字段 | 值 |
|---|---|
| SourceRegion | 0 (#1) / Deserted Mines Entrance (#10) |
| DestinationRegion | D201 (#136) / Entrance Landing (#500) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2528 · D201 (#136) / Entrance Door (#499) / 0 (#1) / Deserted Mines Landing (#11)

| 字段 | 值 |
|---|---|
| SourceRegion | D201 (#136) / Entrance Door (#499) |
| DestinationRegion | 0 (#1) / Deserted Mines Landing (#11) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2529 · D201 (#136) / Floor 2 Door (#501) / D202 (#137) / Floor 1 Landing (#509)

| 字段 | 值 |
|---|---|
| SourceRegion | D201 (#136) / Floor 2 Door (#501) |
| DestinationRegion | D202 (#137) / Floor 1 Landing (#509) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2530 · D202 (#137) / Floor 1 Door (#508) / D201 (#136) / Floor 2 Landing (#502)

| 字段 | 值 |
|---|---|
| SourceRegion | D202 (#137) / Floor 1 Door (#508) |
| DestinationRegion | D201 (#136) / Floor 2 Landing (#502) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2531 · D202 (#137) / Floor 3 Door (#510) / D203 (#138) / Landing (#517)

| 字段 | 值 |
|---|---|
| SourceRegion | D202 (#137) / Floor 3 Door (#510) |
| DestinationRegion | D203 (#138) / Landing (#517) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2532 · D203 (#138) / Door (#516) / D202 (#137) / Floor 3 Landing (#511)

| 字段 | 值 |
|---|---|
| SourceRegion | D203 (#138) / Door (#516) |
| DestinationRegion | D202 (#137) / Floor 3 Landing (#511) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2533 · 0 (#1) / Sabuk Wall Entrance (#14) / 3 (#7) / Bichon Town Landing (#144)

| 字段 | 值 |
|---|---|
| SourceRegion | 0 (#1) / Sabuk Wall Entrance (#14) |
| DestinationRegion | 3 (#7) / Bichon Town Landing (#144) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2534 · 3 (#7) / Bichon Town Door (#143) / 0 (#1) / Sabuk Wall Landing (#15)

| 字段 | 值 |
|---|---|
| SourceRegion | 3 (#7) / Bichon Town Door (#143) |
| DestinationRegion | 0 (#1) / Sabuk Wall Landing (#15) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2535 · 0 (#1) / Phantom Forest Entrance (#12) / D001 (#12) / Bichon Town Landing (#222)

| 字段 | 值 |
|---|---|
| SourceRegion | 0 (#1) / Phantom Forest Entrance (#12) |
| DestinationRegion | D001 (#12) / Bichon Town Landing (#222) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2536 · D001 (#12) / Bichon Town Door (#221) / 0 (#1) / Phantom Forest Landing (#13)

| 字段 | 值 |
|---|---|
| SourceRegion | D001 (#12) / Bichon Town Door (#221) |
| DestinationRegion | 0 (#1) / Phantom Forest Landing (#13) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2537 · 1 (#5) / Cave Door (#45) / D111 (#39) / Entrance Landing (#521)

| 字段 | 值 |
|---|---|
| SourceRegion | 1 (#5) / Cave Door (#45) |
| DestinationRegion | D111 (#39) / Entrance Landing (#521) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2538 · D111 (#39) / Entrance Door (#520) / 1 (#5) / Cave Landing (#46)

| 字段 | 值 |
|---|---|
| SourceRegion | D111 (#39) / Entrance Door (#520) |
| DestinationRegion | 1 (#5) / Cave Landing (#46) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2539 · D111 (#39) / Floor 2 Door (#522) / D112 (#40) / Floor 1 Landing (#530)

| 字段 | 值 |
|---|---|
| SourceRegion | D111 (#39) / Floor 2 Door (#522) |
| DestinationRegion | D112 (#40) / Floor 1 Landing (#530) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2540 · D111 (#39) / Floor 3 Door (#524) / D113 (#41) / Floor 1 Landing (#537)

| 字段 | 值 |
|---|---|
| SourceRegion | D111 (#39) / Floor 3 Door (#524) |
| DestinationRegion | D113 (#41) / Floor 1 Landing (#537) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2541 · D112 (#40) / Floor 3 Door W (#531) / D113 (#41) / Floor 2 Landing W (#539)

| 字段 | 值 |
|---|---|
| SourceRegion | D112 (#40) / Floor 3 Door W (#531) |
| DestinationRegion | D113 (#41) / Floor 2 Landing W (#539) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2542 · D112 (#40) / Floor 3 Door E (#533) / D113 (#41) / Floor 2 Landing E (#541)

| 字段 | 值 |
|---|---|
| SourceRegion | D112 (#40) / Floor 3 Door E (#533) |
| DestinationRegion | D113 (#41) / Floor 2 Landing E (#541) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2543 · D113 (#41) / Floor 1 Door (#536) / D111 (#39) / Floor 3 Landing (#525)

| 字段 | 值 |
|---|---|
| SourceRegion | D113 (#41) / Floor 1 Door (#536) |
| DestinationRegion | D111 (#39) / Floor 3 Landing (#525) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2544 · D113 (#41) / Floor 2 Door W (#538) / D112 (#40) / Floor 3 Landing W (#532)

| 字段 | 值 |
|---|---|
| SourceRegion | D113 (#41) / Floor 2 Door W (#538) |
| DestinationRegion | D112 (#40) / Floor 3 Landing W (#532) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2545 · D113 (#41) / Floor 2 Door E (#540) / D112 (#40) / Floor 3 Landing E (#534)

| 字段 | 值 |
|---|---|
| SourceRegion | D113 (#41) / Floor 2 Door E (#540) |
| DestinationRegion | D112 (#40) / Floor 3 Landing E (#534) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2546 · 1 (#5) / Paradise Forst Door (#47) / D003 (#14) / Lost Paradise Landing (#251)

| 字段 | 值 |
|---|---|
| SourceRegion | 1 (#5) / Paradise Forst Door (#47) |
| DestinationRegion | D003 (#14) / Lost Paradise Landing (#251) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2547 · D003 (#14) / Lost Paradise Door (#250) / 1 (#5) / Paradise Forst Landing (#48)

| 字段 | 值 |
|---|---|
| SourceRegion | D003 (#14) / Lost Paradise Door (#250) |
| DestinationRegion | 1 (#5) / Paradise Forst Landing (#48) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2548 · 1 (#5) / Stone Cave Door (#51) / D701 (#155) / Entrance Landing (#544)

| 字段 | 值 |
|---|---|
| SourceRegion | 1 (#5) / Stone Cave Door (#51) |
| DestinationRegion | D701 (#155) / Entrance Landing (#544) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2549 · D701 (#155) / Entrance Door (#543) / 1 (#5) / Stone Cave Landing (#52)

| 字段 | 值 |
|---|---|
| SourceRegion | D701 (#155) / Entrance Door (#543) |
| DestinationRegion | 1 (#5) / Stone Cave Landing (#52) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2550 · D701 (#155) / Floor 2 Door (#545) / D702 (#156) / Floor 1 Landing (#549)

| 字段 | 值 |
|---|---|
| SourceRegion | D701 (#155) / Floor 2 Door (#545) |
| DestinationRegion | D702 (#156) / Floor 1 Landing (#549) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2551 · D702 (#156) / Floor 1 Door (#548) / D701 (#155) / Floor 2 Landing (#546)

| 字段 | 值 |
|---|---|
| SourceRegion | D702 (#156) / Floor 1 Door (#548) |
| DestinationRegion | D701 (#155) / Floor 2 Landing (#546) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2552 · D702 (#156) / Floor 3 Door (#550) / D703 (#157) / Floor 2 Landing (#554)

| 字段 | 值 |
|---|---|
| SourceRegion | D702 (#156) / Floor 3 Door (#550) |
| DestinationRegion | D703 (#157) / Floor 2 Landing (#554) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2553 · D703 (#157) / Floor 2 Door (#553) / D702 (#156) / Floor 3 Landing (#551)

| 字段 | 值 |
|---|---|
| SourceRegion | D703 (#157) / Floor 2 Door (#553) |
| DestinationRegion | D702 (#156) / Floor 3 Landing (#551) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2554 · D703 (#157) / Floor 4 Door (#555) / D704 (#158) / Floor 3 Landing (#559)

| 字段 | 值 |
|---|---|
| SourceRegion | D703 (#157) / Floor 4 Door (#555) |
| DestinationRegion | D704 (#158) / Floor 3 Landing (#559) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2555 · D704 (#158) / Floor 3 Door (#558) / D703 (#157) / Floor 4 Landing (#556)

| 字段 | 值 |
|---|---|
| SourceRegion | D704 (#158) / Floor 3 Door (#558) |
| DestinationRegion | D703 (#157) / Floor 4 Landing (#556) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2556 · D704 (#158) / Floor 5 Door (#560) / D705 (#159) / Landing (#565)

| 字段 | 值 |
|---|---|
| SourceRegion | D704 (#158) / Floor 5 Door (#560) |
| DestinationRegion | D705 (#159) / Landing (#565) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2557 · 1 (#5) / Mud Wall Door (#55) / 5 (#9) / Lost Paradise Landing (#205)

| 字段 | 值 |
|---|---|
| SourceRegion | 1 (#5) / Mud Wall Door (#55) |
| DestinationRegion | 5 (#9) / Lost Paradise Landing (#205) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2558 · 5 (#9) / Lost Paradise Door (#204) / 1 (#5) / Mud Wall Landing (#56)

| 字段 | 值 |
|---|---|
| SourceRegion | 5 (#9) / Lost Paradise Door (#204) |
| DestinationRegion | 1 (#5) / Mud Wall Landing (#56) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2559 · 1 (#5) / Desert Door (#57) / D002 (#13) / Lost Paradise Landing (#237)

| 字段 | 值 |
|---|---|
| SourceRegion | 1 (#5) / Desert Door (#57) |
| DestinationRegion | D002 (#13) / Lost Paradise Landing (#237) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2560 · D002 (#13) / Lost Paradise Door (#236) / 1 (#5) / Desert Landing (#58)

| 字段 | 值 |
|---|---|
| SourceRegion | D002 (#13) / Lost Paradise Door (#236) |
| DestinationRegion | 1 (#5) / Desert Landing (#58) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2561 · 1 (#5) / Uma Door (#59) / D501 (#146) / Entrance Landing (#568)

| 字段 | 值 |
|---|---|
| SourceRegion | 1 (#5) / Uma Door (#59) |
| DestinationRegion | D501 (#146) / Entrance Landing (#568) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2562 · D501 (#146) / Entrance Door (#567) / 1 (#5) / Uma Landing  (#60)

| 字段 | 值 |
|---|---|
| SourceRegion | D501 (#146) / Entrance Door (#567) |
| DestinationRegion | 1 (#5) / Uma Landing  (#60) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2563 · D501 (#146) / Floor 2 Door (#569) / D502 (#147) / Floor 1 Landing (#576)

| 字段 | 值 |
|---|---|
| SourceRegion | D501 (#146) / Floor 2 Door (#569) |
| DestinationRegion | D502 (#147) / Floor 1 Landing (#576) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2564 · D502 (#147) / Floor 1 Door (#575) / D501 (#146) / Floor 2 Landing (#570)

| 字段 | 值 |
|---|---|
| SourceRegion | D502 (#147) / Floor 1 Door (#575) |
| DestinationRegion | D501 (#146) / Floor 2 Landing (#570) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2565 · D502 (#147) / Floor 3 Door (#577) / D503 (#148) / Floor 2 Landing (#584)

| 字段 | 值 |
|---|---|
| SourceRegion | D502 (#147) / Floor 3 Door (#577) |
| DestinationRegion | D503 (#148) / Floor 2 Landing (#584) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2566 · D503 (#148) / Floor 2 Door (#583) / D502 (#147) / Floor 3 Landing  (#578)

| 字段 | 值 |
|---|---|
| SourceRegion | D503 (#148) / Floor 2 Door (#583) |
| DestinationRegion | D502 (#147) / Floor 3 Landing  (#578) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2567 · D503 (#148) / Floor 4 Door (#585) / D504 (#149) / Landing (#591)

| 字段 | 值 |
|---|---|
| SourceRegion | D503 (#148) / Floor 4 Door (#585) |
| DestinationRegion | D504 (#149) / Landing (#591) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2568 · 3 (#7) / Zuma Temple Door (#141) / D1101 (#33) / Sabuk Landing (#389)

| 字段 | 值 |
|---|---|
| SourceRegion | 3 (#7) / Zuma Temple Door (#141) |
| DestinationRegion | D1101 (#33) / Sabuk Landing (#389) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2569 · D1101 (#33) / Sabuk Door (#388) / 3 (#7) / Zuma Temple Landing (#142)

| 字段 | 值 |
|---|---|
| SourceRegion | D1101 (#33) / Sabuk Door (#388) |
| DestinationRegion | 3 (#7) / Zuma Temple Landing (#142) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2571 · 3 (#7) / Phantom Forest Door (#147) / D001 (#12) / Sabuk Wall Landing (#595)

| 字段 | 值 |
|---|---|
| SourceRegion | 3 (#7) / Phantom Forest Door (#147) |
| DestinationRegion | D001 (#12) / Sabuk Wall Landing (#595) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2572 · D001 (#12) / Sabuk Wall Door (#594) / 3 (#7) / Phantom Forest Landing (#148)

| 字段 | 值 |
|---|---|
| SourceRegion | D001 (#12) / Sabuk Wall Door (#594) |
| DestinationRegion | 3 (#7) / Phantom Forest Landing (#148) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2573 · 3 (#7) / Banya Temple Door (#149) / D1001 (#16) / Sabuk Wall Landing (#263)

| 字段 | 值 |
|---|---|
| SourceRegion | 3 (#7) / Banya Temple Door (#149) |
| DestinationRegion | D1001 (#16) / Sabuk Wall Landing (#263) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2574 · D1001 (#16) / Sabuk Wall Door (#262) / 3 (#7) / Banya Temple Landing (#150)

| 字段 | 值 |
|---|---|
| SourceRegion | D1001 (#16) / Sabuk Wall Door (#262) |
| DestinationRegion | 3 (#7) / Banya Temple Landing (#150) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2575 · 3 (#7) / Banya Village Door (#151) / 2 (#6) / Sabuk Wall Landing (#120)

| 字段 | 值 |
|---|---|
| SourceRegion | 3 (#7) / Banya Village Door (#151) |
| DestinationRegion | 2 (#6) / Sabuk Wall Landing (#120) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2577 · 3 (#7) / Red Moon Door (#145) / D901 (#165) / Sabuk Landing (#598)

| 字段 | 值 |
|---|---|
| SourceRegion | 3 (#7) / Red Moon Door (#145) |
| DestinationRegion | D901 (#165) / Sabuk Landing (#598) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2578 · D901 (#165) / Sabuk Door (#597) / 3 (#7) / Red Moon Landing (#146)

| 字段 | 值 |
|---|---|
| SourceRegion | D901 (#165) / Sabuk Door (#597) |
| DestinationRegion | 3 (#7) / Red Moon Landing (#146) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2579 · D1101 (#33) / Phantom Forest Door (#390) / D001 (#12) / Zuma Temple Landing (#220)

| 字段 | 值 |
|---|---|
| SourceRegion | D1101 (#33) / Phantom Forest Door (#390) |
| DestinationRegion | D001 (#12) / Zuma Temple Landing (#220) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2580 · D001 (#12) / Zuma Temple Door (#219) / D1101 (#33) / Phantom Forest Landing (#391)

| 字段 | 值 |
|---|---|
| SourceRegion | D001 (#12) / Zuma Temple Door (#219) |
| DestinationRegion | D1101 (#33) / Phantom Forest Landing (#391) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2581 · D1101 (#33) / Floor 2 Door (#399) / D1102 (#34) / Floor 1 Landing (#396)

| 字段 | 值 |
|---|---|
| SourceRegion | D1101 (#33) / Floor 2 Door (#399) |
| DestinationRegion | D1102 (#34) / Floor 1 Landing (#396) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2582 · D1102 (#34) / Floor 1 Door (#395) / D1101 (#33) / Floor 2 Landing (#400)

| 字段 | 值 |
|---|---|
| SourceRegion | D1102 (#34) / Floor 1 Door (#395) |
| DestinationRegion | D1101 (#33) / Floor 2 Landing (#400) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2583 · D1102 (#34) / Floor 3 Door (#397) / D1103 (#35) / Floor 2 Landing (#405)

| 字段 | 值 |
|---|---|
| SourceRegion | D1102 (#34) / Floor 3 Door (#397) |
| DestinationRegion | D1103 (#35) / Floor 2 Landing (#405) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2584 · D1103 (#35) / Floor 2 Door (#404) / D1102 (#34) / Floor 3 Landing (#398)

| 字段 | 值 |
|---|---|
| SourceRegion | D1103 (#35) / Floor 2 Door (#404) |
| DestinationRegion | D1102 (#34) / Floor 3 Landing (#398) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2585 · D1103 (#35) / Floor 4 Door (#402) / D1104 (#36) / Floor 3 Landing (#409)

| 字段 | 值 |
|---|---|
| SourceRegion | D1103 (#35) / Floor 4 Door (#402) |
| DestinationRegion | D1104 (#36) / Floor 3 Landing (#409) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2586 · D1104 (#36) / Floor 3 Door (#408) / D1103 (#35) / Floor 4 Landing (#403)

| 字段 | 值 |
|---|---|
| SourceRegion | D1104 (#36) / Floor 3 Door (#408) |
| DestinationRegion | D1103 (#35) / Floor 4 Landing (#403) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2587 · D1104 (#36) / Floor 5 Door  (#410) / D1105 (#37) / Floor 4 Landing (#415)

| 字段 | 值 |
|---|---|
| SourceRegion | D1104 (#36) / Floor 5 Door  (#410) |
| DestinationRegion | D1105 (#37) / Floor 4 Landing (#415) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2588 · D1105 (#37) / Floor 4 Door (#414) / D1104 (#36) / Floor 5 Landing (#411)

| 字段 | 值 |
|---|---|
| SourceRegion | D1105 (#37) / Floor 4 Door (#414) |
| DestinationRegion | D1104 (#36) / Floor 5 Landing (#411) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2589 · D1105 (#37) / Floor 6 Door (#416) / D1106 (#38) / Landing (#425)

| 字段 | 值 |
|---|---|
| SourceRegion | D1105 (#37) / Floor 6 Door (#416) |
| DestinationRegion | D1106 (#38) / Landing (#425) |
| Icon | Down |
| NeedSpawn | Zuma King (#81) / D1106 (#38) / Zumataurus (#426) (#4367) |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2591 · D901 (#165) / Phantom Forest Door (#599) / D001 (#12) / Red Moon Landing (#224)

| 字段 | 值 |
|---|---|
| SourceRegion | D901 (#165) / Phantom Forest Door (#599) |
| DestinationRegion | D001 (#12) / Red Moon Landing (#224) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2592 · D001 (#12) / Red Moon Door (#223) / D901 (#165) / Phantom Forest Landing (#600)

| 字段 | 值 |
|---|---|
| SourceRegion | D001 (#12) / Red Moon Door (#223) |
| DestinationRegion | D901 (#165) / Phantom Forest Landing (#600) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2593 · D901 (#165) / Floor 2 Door W (#601) / D902 (#166) / Floor 1 Landing W (#607)

| 字段 | 值 |
|---|---|
| SourceRegion | D901 (#165) / Floor 2 Door W (#601) |
| DestinationRegion | D902 (#166) / Floor 1 Landing W (#607) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2594 · D901 (#165) / Floor 2 Door E (#603) / D902 (#166) / Floor 1 Landing E (#609)

| 字段 | 值 |
|---|---|
| SourceRegion | D901 (#165) / Floor 2 Door E (#603) |
| DestinationRegion | D902 (#166) / Floor 1 Landing E (#609) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2595 · D902 (#166) / Floor 1 Door W (#606) / D901 (#165) / Floor 2 Landing W (#602)

| 字段 | 值 |
|---|---|
| SourceRegion | D902 (#166) / Floor 1 Door W (#606) |
| DestinationRegion | D901 (#165) / Floor 2 Landing W (#602) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2596 · D902 (#166) / Floor 1 Door E (#608) / D901 (#165) / Floor 2 Landing E (#604)

| 字段 | 值 |
|---|---|
| SourceRegion | D902 (#166) / Floor 1 Door E (#608) |
| DestinationRegion | D901 (#165) / Floor 2 Landing E (#604) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2597 · D902 (#166) / Floor 3 Door W (#610) / D903 (#167) / Floor 2 Landing W (#616)

| 字段 | 值 |
|---|---|
| SourceRegion | D902 (#166) / Floor 3 Door W (#610) |
| DestinationRegion | D903 (#167) / Floor 2 Landing W (#616) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2598 · D902 (#166) / Floor 3 Door E (#612) / D903 (#167) / Floor 2 Landing E (#618)

| 字段 | 值 |
|---|---|
| SourceRegion | D902 (#166) / Floor 3 Door E (#612) |
| DestinationRegion | D903 (#167) / Floor 2 Landing E (#618) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2599 · D903 (#167) / Floor 2 Door W (#615) / D902 (#166) / Floor 3  Landing W (#611)

| 字段 | 值 |
|---|---|
| SourceRegion | D903 (#167) / Floor 2 Door W (#615) |
| DestinationRegion | D902 (#166) / Floor 3  Landing W (#611) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2600 · D903 (#167) / Floor 2 Door E (#617) / D902 (#166) / Floor 3 Landing E (#613)

| 字段 | 值 |
|---|---|
| SourceRegion | D903 (#167) / Floor 2 Door E (#617) |
| DestinationRegion | D902 (#166) / Floor 3 Landing E (#613) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2601 · D903 (#167) / Floor 4 Door (#619) / D904 (#168) / Floor 3 Landing (#623)

| 字段 | 值 |
|---|---|
| SourceRegion | D903 (#167) / Floor 4 Door (#619) |
| DestinationRegion | D904 (#168) / Floor 3 Landing (#623) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2602 · D904 (#168) / Floor 3 Door (#622) / D903 (#167) / Floor 4 Landing (#620)

| 字段 | 值 |
|---|---|
| SourceRegion | D904 (#168) / Floor 3 Door (#622) |
| DestinationRegion | D903 (#167) / Floor 4 Landing (#620) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2603 · D904 (#168) / Floor 5 Door (#624) / D905 (#559) / Landing (#630)

| 字段 | 值 |
|---|---|
| SourceRegion | D904 (#168) / Floor 5 Door (#624) |
| DestinationRegion | D905 (#559) / Landing (#630) |
| Icon | Down |
| NeedSpawn | Red Moon The Fallen (#75) / D905 (#559) / Red Moon (#710) (#4323) |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2605 · D001 (#12) / Banya Village Door (#225) / 2 (#6) / Phantom Forest Landing (#122)

| 字段 | 值 |
|---|---|
| SourceRegion | D001 (#12) / Banya Village Door (#225) |
| DestinationRegion | 2 (#6) / Phantom Forest Landing (#122) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2606 · 2 (#6) / Phantom Forest Door (#121) / D001 (#12) / Banya Village Landing (#226)

| 字段 | 值 |
|---|---|
| SourceRegion | 2 (#6) / Phantom Forest Door (#121) |
| DestinationRegion | D001 (#12) / Banya Village Landing (#226) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2607 · D001 (#12) / Banya Temple Door (#227) / D1001 (#16) / Phantom Forest Landing (#265)

| 字段 | 值 |
|---|---|
| SourceRegion | D001 (#12) / Banya Temple Door (#227) |
| DestinationRegion | D1001 (#16) / Phantom Forest Landing (#265) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2608 · D1001 (#16) / Phantom Forst Door (#264) / D001 (#12) / Banya Temple Landing (#228)

| 字段 | 值 |
|---|---|
| SourceRegion | D1001 (#16) / Phantom Forst Door (#264) |
| DestinationRegion | D001 (#12) / Banya Temple Landing (#228) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2609 · D1001 (#16) / Floor 2 Door (#266) / D1002 (#17) / Floor 1 Landing (#274)

| 字段 | 值 |
|---|---|
| SourceRegion | D1001 (#16) / Floor 2 Door (#266) |
| DestinationRegion | D1002 (#17) / Floor 1 Landing (#274) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2610 · D1002 (#17) / Floor 1 Door (#273) / D1001 (#16) / Floor 2 Landing (#267)

| 字段 | 值 |
|---|---|
| SourceRegion | D1002 (#17) / Floor 1 Door (#273) |
| DestinationRegion | D1001 (#16) / Floor 2 Landing (#267) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2611 · D1002 (#17) / Floor 3 Door - E (#275) / D10032 (#19) / Floor 2 Landiing (#291)

| 字段 | 值 |
|---|---|
| SourceRegion | D1002 (#17) / Floor 3 Door - E (#275) |
| DestinationRegion | D10032 (#19) / Floor 2 Landiing (#291) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2612 · D1002 (#17) / Floor 3 Door - W (#277) / D10031 (#18) / Landing (#287)

| 字段 | 值 |
|---|---|
| SourceRegion | D1002 (#17) / Floor 3 Door - W (#277) |
| DestinationRegion | D10031 (#18) / Landing (#287) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2613 · D10031 (#18) / Door (#286) / D1002 (#17) / Floor 3 Landing - W  (#278)

| 字段 | 值 |
|---|---|
| SourceRegion | D10031 (#18) / Door (#286) |
| DestinationRegion | D1002 (#17) / Floor 3 Landing - W  (#278) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2614 · D10032 (#19) / Floor 2 Door (#290) / D1002 (#17) / Floor 3 Landing - E (#276)

| 字段 | 值 |
|---|---|
| SourceRegion | D10032 (#19) / Floor 2 Door (#290) |
| DestinationRegion | D1002 (#17) / Floor 3 Landing - E (#276) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2615 · D10032 (#19) / Floor 4 Door (#292) / D1004 (#20) / Floor 3 Landing - E (#296)

| 字段 | 值 |
|---|---|
| SourceRegion | D10032 (#19) / Floor 4 Door (#292) |
| DestinationRegion | D1004 (#20) / Floor 3 Landing - E (#296) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2616 · D1004 (#20) / Floor 3 Door - E (#295) / D10032 (#19) / Floor 4 Landing (#293)

| 字段 | 值 |
|---|---|
| SourceRegion | D1004 (#20) / Floor 3 Door - E (#295) |
| DestinationRegion | D10032 (#19) / Floor 4 Landing (#293) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2617 · D1004 (#20) / Floor 5 Door (#297) / D1005 (#21) / Landing (#304)

| 字段 | 值 |
|---|---|
| SourceRegion | D1004 (#20) / Floor 5 Door (#297) |
| DestinationRegion | D1005 (#21) / Landing (#304) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2618 · D1005 (#21) / Door (#303) / D1004 (#20) / Floor 5 Landning (#298)

| 字段 | 值 |
|---|---|
| SourceRegion | D1005 (#21) / Door (#303) |
| DestinationRegion | D1004 (#20) / Floor 5 Landning (#298) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2619 · D1006 (#22) / Door (#306) / D1007 (#23) / Hall Landing (#310)

| 字段 | 值 |
|---|---|
| SourceRegion | D1006 (#22) / Door (#306) |
| DestinationRegion | D1007 (#23) / Hall Landing (#310) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2620 · D1007 (#23) / Hall Door (#309) / D1006 (#22) / Landing (#307)

| 字段 | 值 |
|---|---|
| SourceRegion | D1007 (#23) / Hall Door (#309) |
| DestinationRegion | D1006 (#22) / Landing (#307) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2621 · D1007 (#23) / Floor 7 Door (#311) / D1008 (#24) / Floor 6 Landing (#321)

| 字段 | 值 |
|---|---|
| SourceRegion | D1007 (#23) / Floor 7 Door (#311) |
| DestinationRegion | D1008 (#24) / Floor 6 Landing (#321) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2622 · D1008 (#24) / Floor 6 Door (#320) / D1007 (#23) / Floor 7 Landing (#312)

| 字段 | 值 |
|---|---|
| SourceRegion | D1008 (#24) / Floor 6 Door (#320) |
| DestinationRegion | D1007 (#23) / Floor 7 Landing (#312) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2623 · D1008 (#24) / Floor 8 Door (#322) / D1009 (#25) / Floor 7 Landing (#332)

| 字段 | 值 |
|---|---|
| SourceRegion | D1008 (#24) / Floor 8 Door (#322) |
| DestinationRegion | D1009 (#25) / Floor 7 Landing (#332) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2624 · D1009 (#25) / Floor 7 Door (#331) / D1008 (#24) / Floor 8 Landing (#323)

| 字段 | 值 |
|---|---|
| SourceRegion | D1009 (#25) / Floor 7 Door (#331) |
| DestinationRegion | D1008 (#24) / Floor 8 Landing (#323) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2625 · D1009 (#25) / Floor 9 Door E (#333) / D10102 (#28) / Floor 8 Landing (#352)

| 字段 | 值 |
|---|---|
| SourceRegion | D1009 (#25) / Floor 9 Door E (#333) |
| DestinationRegion | D10102 (#28) / Floor 8 Landing (#352) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2626 · D1009 (#25) / Floor 9 Door W (#335) / D10101 (#27) / Floor 8 Landing (#345)

| 字段 | 值 |
|---|---|
| SourceRegion | D1009 (#25) / Floor 9 Door W (#335) |
| DestinationRegion | D10101 (#27) / Floor 8 Landing (#345) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2627 · D1009 (#25) / Floor 10 Door (#337) / D1011 (#29) / Floor 8 Landing (#359)

| 字段 | 值 |
|---|---|
| SourceRegion | D1009 (#25) / Floor 10 Door (#337) |
| DestinationRegion | D1011 (#29) / Floor 8 Landing (#359) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2628 · D10102 (#28) / Floor 8 Door (#351) / D1009 (#25) / Floor 9 Landing E (#334)

| 字段 | 值 |
|---|---|
| SourceRegion | D10102 (#28) / Floor 8 Door (#351) |
| DestinationRegion | D1009 (#25) / Floor 9 Landing E (#334) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2629 · D10102 (#28) / Floor 10 Door (#353) / D1011 (#29) / Floor 9 Landing E (#361)

| 字段 | 值 |
|---|---|
| SourceRegion | D10102 (#28) / Floor 10 Door (#353) |
| DestinationRegion | D1011 (#29) / Floor 9 Landing E (#361) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2630 · D10101 (#27) / Floor 8 Door (#344) / D1009 (#25) / Floor 9 Landing W (#336)

| 字段 | 值 |
|---|---|
| SourceRegion | D10101 (#27) / Floor 8 Door (#344) |
| DestinationRegion | D1009 (#25) / Floor 9 Landing W (#336) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2631 · D10101 (#27) / Floor 10 Door (#346) / D1011 (#29) / Floor 9 Landing W (#363)

| 字段 | 值 |
|---|---|
| SourceRegion | D10101 (#27) / Floor 10 Door (#346) |
| DestinationRegion | D1011 (#29) / Floor 9 Landing W (#363) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2632 · D1011 (#29) / Floor 8 Door (#358) / D1009 (#25) / Floor 10 Landing (#338)

| 字段 | 值 |
|---|---|
| SourceRegion | D1011 (#29) / Floor 8 Door (#358) |
| DestinationRegion | D1009 (#25) / Floor 10 Landing (#338) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2633 · D1011 (#29) / Floor 9 Door E (#360) / D10102 (#28) / Floor 10 Landing (#354)

| 字段 | 值 |
|---|---|
| SourceRegion | D1011 (#29) / Floor 9 Door E (#360) |
| DestinationRegion | D10102 (#28) / Floor 10 Landing (#354) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2634 · D1011 (#29) / Floor 9 Door W (#362) / D10101 (#27) / Floor 10 Landing (#347)

| 字段 | 值 |
|---|---|
| SourceRegion | D1011 (#29) / Floor 9 Door W (#362) |
| DestinationRegion | D10101 (#27) / Floor 10 Landing (#347) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2635 · D1011 (#29) / Floor 11 Door (#364) / D1012 (#30) / Landing (#633)

| 字段 | 值 |
|---|---|
| SourceRegion | D1011 (#29) / Floor 11 Door (#364) |
| DestinationRegion | D1012 (#30) / Landing (#633) |
| Icon | Down |
| NeedSpawn | Emperor Sa'Woo (#115) / D1012 (#30) / Boss Spawn (#634) (#4645) |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2636 · 2 (#6) / Sabuk Wall Door (#119) / 3 (#7) / Banya Village Landing (#152)

| 字段 | 值 |
|---|---|
| SourceRegion | 2 (#6) / Sabuk Wall Door (#119) |
| DestinationRegion | 3 (#7) / Banya Village Landing (#152) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2637 · 2 (#6) / Flea Cave Door (#117) / D301 (#139) / Entrance Landing (#647)

| 字段 | 值 |
|---|---|
| SourceRegion | 2 (#6) / Flea Cave Door (#117) |
| DestinationRegion | D301 (#139) / Entrance Landing (#647) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2638 · D301 (#139) / Entrance Door (#646) / 2 (#6) / Flea Cave Landing (#118)

| 字段 | 值 |
|---|---|
| SourceRegion | D301 (#139) / Entrance Door (#646) |
| DestinationRegion | 2 (#6) / Flea Cave Landing (#118) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2639 · 2 (#6) / Cave Door (#123) / D121 (#59) / Entrance Landing (#665)

| 字段 | 值 |
|---|---|
| SourceRegion | 2 (#6) / Cave Door (#123) |
| DestinationRegion | D121 (#59) / Entrance Landing (#665) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2640 · D121 (#59) / Entance Door (#664) / 2 (#6) / Cave Landing (#124)

| 字段 | 值 |
|---|---|
| SourceRegion | D121 (#59) / Entance Door (#664) |
| DestinationRegion | 2 (#6) / Cave Landing (#124) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2641 · 2 (#6) / Banya South Door (#125) / D004 (#15) / Banya Village Landing (#255)

| 字段 | 值 |
|---|---|
| SourceRegion | 2 (#6) / Banya South Door (#125) |
| DestinationRegion | D004 (#15) / Banya Village Landing (#255) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2642 · D004 (#15) / Banya Village Door (#254) / 2 (#6) / Bany South Landing (#126)

| 字段 | 值 |
|---|---|
| SourceRegion | D004 (#15) / Banya Village Door (#254) |
| DestinationRegion | 2 (#6) / Bany South Landing (#126) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2645 · 2 (#6) / South Way Door (#129) / E12 (#225) / Banya Village Landing (#644)

| 字段 | 值 |
|---|---|
| SourceRegion | 2 (#6) / South Way Door (#129) |
| DestinationRegion | E12 (#225) / Banya Village Landing (#644) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2646 · E12 (#225) / Banya Village Door (#643) / 2 (#6) / South Way Landing (#130)

| 字段 | 值 |
|---|---|
| SourceRegion | E12 (#225) / Banya Village Door (#643) |
| DestinationRegion | 2 (#6) / South Way Landing (#130) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2647 · D121 (#59) / Floor 3 Door (#666) / D123 (#61) / Floor 1 Landing (#679)

| 字段 | 值 |
|---|---|
| SourceRegion | D121 (#59) / Floor 3 Door (#666) |
| DestinationRegion | D123 (#61) / Floor 1 Landing (#679) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2648 · D121 (#59) / Floor 2 Door (#668) / D122 (#60) / Floor 1 Landing (#672)

| 字段 | 值 |
|---|---|
| SourceRegion | D121 (#59) / Floor 2 Door (#668) |
| DestinationRegion | D122 (#60) / Floor 1 Landing (#672) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2649 · D122 (#60) / Floor 1 Door (#671) / D121 (#59) / Floor 2 Landing (#669)

| 字段 | 值 |
|---|---|
| SourceRegion | D122 (#60) / Floor 1 Door (#671) |
| DestinationRegion | D121 (#59) / Floor 2 Landing (#669) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2650 · D122 (#60) / Floor 3 Door W (#673) / D123 (#61) / Floor 2 Landing W (#681)

| 字段 | 值 |
|---|---|
| SourceRegion | D122 (#60) / Floor 3 Door W (#673) |
| DestinationRegion | D123 (#61) / Floor 2 Landing W (#681) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2651 · D122 (#60) / Floor 3 Door E (#675) / D123 (#61) / Floor 2 Landing E (#683)

| 字段 | 值 |
|---|---|
| SourceRegion | D122 (#60) / Floor 3 Door E (#675) |
| DestinationRegion | D123 (#61) / Floor 2 Landing E (#683) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2652 · D123 (#61) / Floor 1 Door (#678) / D121 (#59) / Floor 3 Landing (#667)

| 字段 | 值 |
|---|---|
| SourceRegion | D123 (#61) / Floor 1 Door (#678) |
| DestinationRegion | D121 (#59) / Floor 3 Landing (#667) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2653 · D123 (#61) / Floor 2 Door W (#680) / D122 (#60) / Floor 3 Landing W (#674)

| 字段 | 值 |
|---|---|
| SourceRegion | D123 (#61) / Floor 2 Door W (#680) |
| DestinationRegion | D122 (#60) / Floor 3 Landing W (#674) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2654 · D123 (#61) / Floor 2 Door E (#682) / D122 (#60) / Floor 3 Landing E (#676)

| 字段 | 值 |
|---|---|
| SourceRegion | D123 (#61) / Floor 2 Door E (#682) |
| DestinationRegion | D122 (#60) / Floor 3 Landing E (#676) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2655 · 2 (#6) / Stone Cave Door (#127) / D601 (#150) / Entrance Landing (#685)

| 字段 | 值 |
|---|---|
| SourceRegion | 2 (#6) / Stone Cave Door (#127) |
| DestinationRegion | D601 (#150) / Entrance Landing (#685) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2656 · D601 (#150) / Entrance Door (#684) / 2 (#6) / Stone Cave Landing (#128)

| 字段 | 值 |
|---|---|
| SourceRegion | D601 (#150) / Entrance Door (#684) |
| DestinationRegion | 2 (#6) / Stone Cave Landing (#128) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2657 · D601 (#150) / Floor 2 Door (#686) / D602 (#151) / Floor 1 Landing (#690)

| 字段 | 值 |
|---|---|
| SourceRegion | D601 (#150) / Floor 2 Door (#686) |
| DestinationRegion | D602 (#151) / Floor 1 Landing (#690) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2658 · D602 (#151) / Floor 1 Door (#689) / D601 (#150) / Floor 2 Landing (#687)

| 字段 | 值 |
|---|---|
| SourceRegion | D602 (#151) / Floor 1 Door (#689) |
| DestinationRegion | D601 (#150) / Floor 2 Landing (#687) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2659 · D602 (#151) / Floor 3 Door (#691) / D603 (#152) / Floor 2 Landing (#696)

| 字段 | 值 |
|---|---|
| SourceRegion | D602 (#151) / Floor 3 Door (#691) |
| DestinationRegion | D603 (#152) / Floor 2 Landing (#696) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2660 · D603 (#152) / Floor 2 Door (#695) / D602 (#151) / Floor 3 Landing (#692)

| 字段 | 值 |
|---|---|
| SourceRegion | D603 (#152) / Floor 2 Door (#695) |
| DestinationRegion | D602 (#151) / Floor 3 Landing (#692) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2661 · D603 (#152) / Floor 4 Door (#697) / D604 (#153) / Floor 3 Landing (#702)

| 字段 | 值 |
|---|---|
| SourceRegion | D603 (#152) / Floor 4 Door (#697) |
| DestinationRegion | D604 (#153) / Floor 3 Landing (#702) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2662 · D604 (#153) / Floor 3 Door (#701) / D603 (#152) / Floor 4 Landing (#698)

| 字段 | 值 |
|---|---|
| SourceRegion | D604 (#153) / Floor 3 Door (#701) |
| DestinationRegion | D603 (#152) / Floor 4 Landing (#698) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2663 · D604 (#153) / Floor 5 Door (#703) / D605 (#154) / Floor 4 Landing (#707)

| 字段 | 值 |
|---|---|
| SourceRegion | D604 (#153) / Floor 5 Door (#703) |
| DestinationRegion | D605 (#154) / Floor 4 Landing (#707) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2664 · D605 (#154) / Floor 4 Door (#706) / D604 (#153) / Floor 5 Landing (#704)

| 字段 | 值 |
|---|---|
| SourceRegion | D605 (#154) / Floor 4 Door (#706) |
| DestinationRegion | D604 (#153) / Floor 5 Landing (#704) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2665 · E12 (#225) / Left Door (#641) / E11 (#224) / Right Landing (#637)

| 字段 | 值 |
|---|---|
| SourceRegion | E12 (#225) / Left Door (#641) |
| DestinationRegion | E11 (#224) / Right Landing (#637) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2666 · E11 (#224) / Right Door (#636) / E12 (#225) / Left Landing (#642)

| 字段 | 值 |
|---|---|
| SourceRegion | E11 (#224) / Right Door (#636) |
| DestinationRegion | E12 (#225) / Left Landing (#642) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2667 · E11 (#224) / Numa Village Door (#638) / 4 (#8) / South Way Landing (#177)

| 字段 | 值 |
|---|---|
| SourceRegion | E11 (#224) / Numa Village Door (#638) |
| DestinationRegion | 4 (#8) / South Way Landing (#177) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2668 · 4 (#8) / South Way Door (#176) / E11 (#224) / Numa Village Landing (#639)

| 字段 | 值 |
|---|---|
| SourceRegion | 4 (#8) / South Way Door (#176) |
| DestinationRegion | E11 (#224) / Numa Village Landing (#639) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2669 · 4 (#8) / Desert Door (#172) / D002 (#13) / Numa Village Landing (#243)

| 字段 | 值 |
|---|---|
| SourceRegion | 4 (#8) / Desert Door (#172) |
| DestinationRegion | D002 (#13) / Numa Village Landing (#243) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2670 · 4 (#8) / Mud Wall Door (#174) / 5 (#9) / Numa Village Landing (#211)

| 字段 | 值 |
|---|---|
| SourceRegion | 4 (#8) / Mud Wall Door (#174) |
| DestinationRegion | 5 (#9) / Numa Village Landing (#211) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2671 · 5 (#9) / Desert Door (#202) / D002 (#13) / Mud Wall Landing (#239)

| 字段 | 值 |
|---|---|
| SourceRegion | 5 (#9) / Desert Door (#202) |
| DestinationRegion | D002 (#13) / Mud Wall Landing (#239) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2672 · 5 (#9) / Numa Village Door (#210) / 4 (#8) / Mud Wall Landing (#175)

| 字段 | 值 |
|---|---|
| SourceRegion | 5 (#9) / Numa Village Door (#210) |
| DestinationRegion | 4 (#8) / Mud Wall Landing (#175) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2673 · D002 (#13) / Mud Wall Door (#238) / 5 (#9) / Desert Landing (#203)

| 字段 | 值 |
|---|---|
| SourceRegion | D002 (#13) / Mud Wall Door (#238) |
| DestinationRegion | 5 (#9) / Desert Landing (#203) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2674 · D002 (#13) / Numa Village Door (#242) / 4 (#8) / Desert Landing (#173)

| 字段 | 值 |
|---|---|
| SourceRegion | D002 (#13) / Numa Village Door (#242) |
| DestinationRegion | 4 (#8) / Desert Landing (#173) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2675 · D1401 (#68) / Boat Door (#713) / D1402 (#69) / Boat Landing (#717)

| 字段 | 值 |
|---|---|
| SourceRegion | D1401 (#68) / Boat Door (#713) |
| DestinationRegion | D1402 (#69) / Boat Landing (#717) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2676 · D1402 (#69) / Floor 3 Door (#718) / D1403 (#70) / Floor 4 Landing (#723)

| 字段 | 值 |
|---|---|
| SourceRegion | D1402 (#69) / Floor 3 Door (#718) |
| DestinationRegion | D1403 (#70) / Floor 4 Landing (#723) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2677 · D1403 (#70) / Floor 4 Door (#722) / D1402 (#69) / Floor 3 Landing (#719)

| 字段 | 值 |
|---|---|
| SourceRegion | D1403 (#70) / Floor 4 Door (#722) |
| DestinationRegion | D1402 (#69) / Floor 3 Landing (#719) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2678 · D1403 (#70) / Floor 2 Door  (#724) / D1404 (#71) / Floor 3 Landing (#729)

| 字段 | 值 |
|---|---|
| SourceRegion | D1403 (#70) / Floor 2 Door  (#724) |
| DestinationRegion | D1404 (#71) / Floor 3 Landing (#729) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2679 · D1404 (#71) / Floor 3 Door (#728) / D1403 (#70) / Floor 2 Landing (#725)

| 字段 | 值 |
|---|---|
| SourceRegion | D1404 (#71) / Floor 3 Door (#728) |
| DestinationRegion | D1403 (#70) / Floor 2 Landing (#725) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2680 · D1404 (#71) / Floor 1 Door (#730) / D1405 (#72) / Floor 2 Landing (#735)

| 字段 | 值 |
|---|---|
| SourceRegion | D1404 (#71) / Floor 1 Door (#730) |
| DestinationRegion | D1405 (#72) / Floor 2 Landing (#735) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2681 · D1405 (#72) / Floor 2 Door (#734) / D1404 (#71) / Floor 1 Landing (#731)

| 字段 | 值 |
|---|---|
| SourceRegion | D1405 (#72) / Floor 2 Door (#734) |
| DestinationRegion | D1404 (#71) / Floor 1 Landing (#731) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2682 · D1405 (#72) / Flight Deck Door (#736) / D1406 (#73) / Landing (#743)

| 字段 | 值 |
|---|---|
| SourceRegion | D1405 (#72) / Flight Deck Door (#736) |
| DestinationRegion | D1406 (#73) / Landing (#743) |
| Icon | Down |
| NeedItem | Yun Wine (#623) |
| NeedSpawn | Pachon The Chaos bringer (#160) / D1406 (#73) / Boss Spawn (#745) (#4914) |
| NeedHole | false |
| Effect | SpecialRepair |
| RequiredClass | All |
| SkipValidation | false |

### #2683 · D002 (#13) / West Deset Door (#244) / D2001 (#125) / Entrance Landing (#748)

| 字段 | 值 |
|---|---|
| SourceRegion | D002 (#13) / West Deset Door (#244) |
| DestinationRegion | D2001 (#125) / Entrance Landing (#748) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2684 · D2001 (#125) / Entrance Door (#747) / D002 (#13) / West Desert Landing (#245)

| 字段 | 值 |
|---|---|
| SourceRegion | D2001 (#125) / Entrance Door (#747) |
| DestinationRegion | D002 (#13) / West Desert Landing (#245) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2685 · D2001 (#125) / Floor 2 Door (#749) / D20011 (#126) / Floor 1 Landing (#753)

| 字段 | 值 |
|---|---|
| SourceRegion | D2001 (#125) / Floor 2 Door (#749) |
| DestinationRegion | D20011 (#126) / Floor 1 Landing (#753) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2686 · D20011 (#126) / Floor 1 Door (#752) / D2001 (#125) / Floor 2 Landing (#750)

| 字段 | 值 |
|---|---|
| SourceRegion | D20011 (#126) / Floor 1 Door (#752) |
| DestinationRegion | D2001 (#125) / Floor 2 Landing (#750) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2687 · D20011 (#126) / Floor 3 Door (#754) / D20012 (#127) / Floor 2 Landing (#758)

| 字段 | 值 |
|---|---|
| SourceRegion | D20011 (#126) / Floor 3 Door (#754) |
| DestinationRegion | D20012 (#127) / Floor 2 Landing (#758) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2688 · D20012 (#127) / Floor 2 Door (#757) / D20011 (#126) / Floor 3 Landing (#755)

| 字段 | 值 |
|---|---|
| SourceRegion | D20012 (#127) / Floor 2 Door (#757) |
| DestinationRegion | D20011 (#126) / Floor 3 Landing (#755) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2689 · D20012 (#127) / City Door (#759) / D2002 (#128) / Floor 3 Landing (#763)

| 字段 | 值 |
|---|---|
| SourceRegion | D20012 (#127) / City Door (#759) |
| DestinationRegion | D2002 (#128) / Floor 3 Landing (#763) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2690 · D2002 (#128) / Floor 3 Door (#762) / D20012 (#127) / City Landing (#760)

| 字段 | 值 |
|---|---|
| SourceRegion | D2002 (#128) / Floor 3 Door (#762) |
| DestinationRegion | D20012 (#127) / City Landing (#760) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2691 · D2002 (#128) / City 2 Door (#764) / D20021 (#129) / City 1 Landing (#769)

| 字段 | 值 |
|---|---|
| SourceRegion | D2002 (#128) / City 2 Door (#764) |
| DestinationRegion | D20021 (#129) / City 1 Landing (#769) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2692 · D2002 (#128) / Trap (#766) / D2002 (#128) / Whole Map (#761)

| 字段 | 值 |
|---|---|
| SourceRegion | D2002 (#128) / Trap (#766) |
| DestinationRegion | D2002 (#128) / Whole Map (#761) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2693 · D20021 (#129) / City 1 Door (#768) / D2002 (#128) / City 2 Landing (#765)

| 字段 | 值 |
|---|---|
| SourceRegion | D20021 (#129) / City 1 Door (#768) |
| DestinationRegion | D2002 (#128) / City 2 Landing (#765) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2694 · D20021 (#129) / City 3 Door (#770) / D20022 (#130) / City 2 Landing (#782)

| 字段 | 值 |
|---|---|
| SourceRegion | D20021 (#129) / City 3 Door (#770) |
| DestinationRegion | D20022 (#130) / City 2 Landing (#782) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2695 · D20021 (#129) / Mine 1 Door (#772) / D2003 (#132) / City 2 Landing (#795)

| 字段 | 值 |
|---|---|
| SourceRegion | D20021 (#129) / Mine 1 Door (#772) |
| DestinationRegion | D2003 (#132) / City 2 Landing (#795) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2696 · D20021 (#129) / Traps (#774) / D20021 (#129) / Whole Map (#767)

| 字段 | 值 |
|---|---|
| SourceRegion | D20021 (#129) / Traps (#774) |
| DestinationRegion | D20021 (#129) / Whole Map (#767) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2697 · D20022 (#130) / City 2 Door (#781) / D20021 (#129) / City 3 Landing (#771)

| 字段 | 值 |
|---|---|
| SourceRegion | D20022 (#130) / City 2 Door (#781) |
| DestinationRegion | D20021 (#129) / City 3 Landing (#771) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2698 · D20022 (#130) / City 4 Door (#783) / D20023 (#131) / City 3 Landing (#787)

| 字段 | 值 |
|---|---|
| SourceRegion | D20022 (#130) / City 4 Door (#783) |
| DestinationRegion | D20023 (#131) / City 3 Landing (#787) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2699 · D20023 (#131) / City 3 Door (#786) / D20022 (#130) / City 4 Landing (#784)

| 字段 | 值 |
|---|---|
| SourceRegion | D20023 (#131) / City 3 Door (#786) |
| DestinationRegion | D20022 (#130) / City 4 Landing (#784) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2700 · D20023 (#131) / Royal Room Door (#788) / D2004 (#135) / Landing (#813)

| 字段 | 值 |
|---|---|
| SourceRegion | D20023 (#131) / Royal Room Door (#788) |
| DestinationRegion | D2004 (#135) / Landing (#813) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2701 · D20023 (#131) / Traps (#790) / D20023 (#131) / Whole Map (#785)

| 字段 | 值 |
|---|---|
| SourceRegion | D20023 (#131) / Traps (#790) |
| DestinationRegion | D20023 (#131) / Whole Map (#785) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2702 · D2003 (#132) / City 2 Door (#794) / D20021 (#129) / Mine 1 Landing (#773)

| 字段 | 值 |
|---|---|
| SourceRegion | D2003 (#132) / City 2 Door (#794) |
| DestinationRegion | D20021 (#129) / Mine 1 Landing (#773) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2703 · D2003 (#132) / Mine 2 Door (#796) / D20031 (#133) / Mine 1 Landing (#800)

| 字段 | 值 |
|---|---|
| SourceRegion | D2003 (#132) / Mine 2 Door (#796) |
| DestinationRegion | D20031 (#133) / Mine 1 Landing (#800) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2704 · D20031 (#133) / Mine 1 Door (#799) / D2003 (#132) / Mine 2 Landing (#797)

| 字段 | 值 |
|---|---|
| SourceRegion | D20031 (#133) / Mine 1 Door (#799) |
| DestinationRegion | D2003 (#132) / Mine 2 Landing (#797) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2705 · D20031 (#133) / Top Right Door (#801) / D20031 (#133) / Bottom Left Landing (#805)

| 字段 | 值 |
|---|---|
| SourceRegion | D20031 (#133) / Top Right Door (#801) |
| DestinationRegion | D20031 (#133) / Bottom Left Landing (#805) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2706 · D20031 (#133) / Bottom Left Door (#804) / D20031 (#133) / Top Right Landing (#802)

| 字段 | 值 |
|---|---|
| SourceRegion | D20031 (#133) / Bottom Left Door (#804) |
| DestinationRegion | D20031 (#133) / Top Right Landing (#802) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2707 · D20031 (#133) / Mine 3 Door (#807) / D20032 (#134) / Mine 2 Landing (#810)

| 字段 | 值 |
|---|---|
| SourceRegion | D20031 (#133) / Mine 3 Door (#807) |
| DestinationRegion | D20032 (#134) / Mine 2 Landing (#810) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2708 · D20032 (#134) / Mine 2 Door (#809) / D20031 (#133) / Mine 3 Landing (#808)

| 字段 | 值 |
|---|---|
| SourceRegion | D20032 (#134) / Mine 2 Door (#809) |
| DestinationRegion | D20031 (#133) / Mine 3 Landing (#808) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2709 · D003 (#14) / Homeland Door (#246) / D005 (#242) / Lost Paradise Forest Landing (#817)

| 字段 | 值 |
|---|---|
| SourceRegion | D003 (#14) / Homeland Door (#246) |
| DestinationRegion | D005 (#242) / Lost Paradise Forest Landing (#817) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2710 · D005 (#242) / Lost Paradise Forest Door (#816) / D003 (#14) / HomeLand Landing (#247)

| 字段 | 值 |
|---|---|
| SourceRegion | D005 (#242) / Lost Paradise Forest Door (#816) |
| DestinationRegion | D003 (#14) / HomeLand Landing (#247) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2711 · D005 (#242) / Frost Village Door (#818) / 8 (#241) / Homeland Landing (#838)

| 字段 | 值 |
|---|---|
| SourceRegion | D005 (#242) / Frost Village Door (#818) |
| DestinationRegion | 8 (#241) / Homeland Landing (#838) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2712 · 8 (#241) / Homeland Door (#837) / D005 (#242) / Frost Village Landing (#819)

| 字段 | 值 |
|---|---|
| SourceRegion | 8 (#241) / Homeland Door (#837) |
| DestinationRegion | D005 (#242) / Frost Village Landing (#819) |
| Icon | Province |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2713 · 8 (#241) / Frost Dungeon Door (#839) / D2101 (#243) / Entrance Landing (#848)

| 字段 | 值 |
|---|---|
| SourceRegion | 8 (#241) / Frost Dungeon Door (#839) |
| DestinationRegion | D2101 (#243) / Entrance Landing (#848) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2714 · D2101 (#243) / Entrance Door (#847) / 8 (#241) / Frost Dungeon Landing (#840)

| 字段 | 值 |
|---|---|
| SourceRegion | D2101 (#243) / Entrance Door (#847) |
| DestinationRegion | 8 (#241) / Frost Dungeon Landing (#840) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2715 · D2101 (#243) / Floor 2 Door (#849) / D2102 (#244) / Floor 1 Landing (#853)

| 字段 | 值 |
|---|---|
| SourceRegion | D2101 (#243) / Floor 2 Door (#849) |
| DestinationRegion | D2102 (#244) / Floor 1 Landing (#853) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2716 · D2102 (#244) / Floor 1 Door (#852) / D2101 (#243) / Floor 2 Landing (#850)

| 字段 | 值 |
|---|---|
| SourceRegion | D2102 (#244) / Floor 1 Door (#852) |
| DestinationRegion | D2101 (#243) / Floor 2 Landing (#850) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2717 · D2102 (#244) / Floor 3 Door (#854) / D2103 (#245) / Floor 2 Landing (#858)

| 字段 | 值 |
|---|---|
| SourceRegion | D2102 (#244) / Floor 3 Door (#854) |
| DestinationRegion | D2103 (#245) / Floor 2 Landing (#858) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2718 · D2103 (#245) / Floor 2 Door (#857) / D2102 (#244) / Floor 3 Landing (#855)

| 字段 | 值 |
|---|---|
| SourceRegion | D2103 (#245) / Floor 2 Door (#857) |
| DestinationRegion | D2102 (#244) / Floor 3 Landing (#855) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2719 · D2103 (#245) / Floor 4 Door (#859) / D2104 (#246) / Floor 3 Landing (#863)

| 字段 | 值 |
|---|---|
| SourceRegion | D2103 (#245) / Floor 4 Door (#859) |
| DestinationRegion | D2104 (#246) / Floor 3 Landing (#863) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2720 · D2104 (#246) / Floor 3 Door (#862) / D2103 (#245) / Floor 4 Landing (#860)

| 字段 | 值 |
|---|---|
| SourceRegion | D2104 (#246) / Floor 3 Door (#862) |
| DestinationRegion | D2103 (#245) / Floor 4 Landing (#860) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2721 · D2104 (#246) / Floor 5 Door (#864) / D21051 (#247) / Top Landing (#869)

| 字段 | 值 |
|---|---|
| SourceRegion | D2104 (#246) / Floor 5 Door (#864) |
| DestinationRegion | D21051 (#247) / Top Landing (#869) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2722 · D21051 (#247) / Top Door (#868) / D21051 (#247) / Bottom Landing (#873)

| 字段 | 值 |
|---|---|
| SourceRegion | D21051 (#247) / Top Door (#868) |
| DestinationRegion | D21051 (#247) / Bottom Landing (#873) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2723 · D21051 (#247) / Right Door (#870) / D21051 (#247) / Left Landing (#875)

| 字段 | 值 |
|---|---|
| SourceRegion | D21051 (#247) / Right Door (#870) |
| DestinationRegion | D21051 (#247) / Left Landing (#875) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2724 · D21051 (#247) / Bottom Door (#872) / D21052 (#248) / Top Landing (#879)

| 字段 | 值 |
|---|---|
| SourceRegion | D21051 (#247) / Bottom Door (#872) |
| DestinationRegion | D21052 (#248) / Top Landing (#879) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2725 · D21051 (#247) / Left Door (#874) / D21051 (#247) / Right Landing (#871)

| 字段 | 值 |
|---|---|
| SourceRegion | D21051 (#247) / Left Door (#874) |
| DestinationRegion | D21051 (#247) / Right Landing (#871) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2726 · D21052 (#248) / Top Door (#878) / D21051 (#247) / Bottom Landing (#873)

| 字段 | 值 |
|---|---|
| SourceRegion | D21052 (#248) / Top Door (#878) |
| DestinationRegion | D21051 (#247) / Bottom Landing (#873) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2727 · D21052 (#248) / Right Door (#880) / D21053 (#249) / Left Landing (#894)

| 字段 | 值 |
|---|---|
| SourceRegion | D21052 (#248) / Right Door (#880) |
| DestinationRegion | D21053 (#249) / Left Landing (#894) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2728 · D21052 (#248) / Bottom Door (#882) / D21051 (#247) / Top Landing (#869)

| 字段 | 值 |
|---|---|
| SourceRegion | D21052 (#248) / Bottom Door (#882) |
| DestinationRegion | D21051 (#247) / Top Landing (#869) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2729 · D21052 (#248) / Left Door (#884) / D21051 (#247) / Right Landing (#871)

| 字段 | 值 |
|---|---|
| SourceRegion | D21052 (#248) / Left Door (#884) |
| DestinationRegion | D21051 (#247) / Right Landing (#871) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2730 · D21053 (#249) / Top Door (#887) / D21051 (#247) / Bottom Landing (#873)

| 字段 | 值 |
|---|---|
| SourceRegion | D21053 (#249) / Top Door (#887) |
| DestinationRegion | D21051 (#247) / Bottom Landing (#873) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2731 · D21053 (#249) / Right Door (#889) / D21051 (#247) / Left Landing (#875)

| 字段 | 值 |
|---|---|
| SourceRegion | D21053 (#249) / Right Door (#889) |
| DestinationRegion | D21051 (#247) / Left Landing (#875) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2732 · D21053 (#249) / Bottom Door (#891) / D21054 (#250) / Top Landing (#897)

| 字段 | 值 |
|---|---|
| SourceRegion | D21053 (#249) / Bottom Door (#891) |
| DestinationRegion | D21054 (#250) / Top Landing (#897) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2733 · D21053 (#249) / Left Door (#893) / D21051 (#247) / Right Landing (#871)

| 字段 | 值 |
|---|---|
| SourceRegion | D21053 (#249) / Left Door (#893) |
| DestinationRegion | D21051 (#247) / Right Landing (#871) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2734 · D21054 (#250) / Top Door (#896) / D21051 (#247) / Bottom Landing (#873)

| 字段 | 值 |
|---|---|
| SourceRegion | D21054 (#250) / Top Door (#896) |
| DestinationRegion | D21051 (#247) / Bottom Landing (#873) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2735 · D21054 (#250) / Right Door (#898) / D21051 (#247) / Left Landing (#875)

| 字段 | 值 |
|---|---|
| SourceRegion | D21054 (#250) / Right Door (#898) |
| DestinationRegion | D21051 (#247) / Left Landing (#875) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2736 · D21054 (#250) / Bottom Door (#900) / D21051 (#247) / Top Landing (#869)

| 字段 | 值 |
|---|---|
| SourceRegion | D21054 (#250) / Bottom Door (#900) |
| DestinationRegion | D21051 (#247) / Top Landing (#869) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2737 · D21054 (#250) / Left Door (#902) / D21055 (#254) / Right Landing (#913)

| 字段 | 值 |
|---|---|
| SourceRegion | D21054 (#250) / Left Door (#902) |
| DestinationRegion | D21055 (#254) / Right Landing (#913) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2738 · D21055 (#254) / Top Door (#910) / D21056 (#255) / Bottom Landing (#924)

| 字段 | 值 |
|---|---|
| SourceRegion | D21055 (#254) / Top Door (#910) |
| DestinationRegion | D21056 (#255) / Bottom Landing (#924) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2739 · D21055 (#254) / Right Door (#912) / D21051 (#247) / Left Landing (#875)

| 字段 | 值 |
|---|---|
| SourceRegion | D21055 (#254) / Right Door (#912) |
| DestinationRegion | D21051 (#247) / Left Landing (#875) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2740 · D21055 (#254) / Bottom Door (#914) / D21051 (#247) / Top Landing (#869)

| 字段 | 值 |
|---|---|
| SourceRegion | D21055 (#254) / Bottom Door (#914) |
| DestinationRegion | D21051 (#247) / Top Landing (#869) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2741 · D21055 (#254) / Left Door (#916) / D21051 (#247) / Right Landing (#871)

| 字段 | 值 |
|---|---|
| SourceRegion | D21055 (#254) / Left Door (#916) |
| DestinationRegion | D21051 (#247) / Right Landing (#871) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2742 · D21056 (#255) / Top Door (#919) / D21051 (#247) / Bottom Landing (#873)

| 字段 | 值 |
|---|---|
| SourceRegion | D21056 (#255) / Top Door (#919) |
| DestinationRegion | D21051 (#247) / Bottom Landing (#873) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2743 · D21056 (#255) / Right Door (#921) / D2106 (#251) / Landing (#906)

| 字段 | 值 |
|---|---|
| SourceRegion | D21056 (#255) / Right Door (#921) |
| DestinationRegion | D2106 (#251) / Landing (#906) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2744 · D21056 (#255) / Bottom Door (#923) / D21051 (#247) / Top Landing (#869)

| 字段 | 值 |
|---|---|
| SourceRegion | D21056 (#255) / Bottom Door (#923) |
| DestinationRegion | D21051 (#247) / Top Landing (#869) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2745 · D21056 (#255) / Left Door (#925) / D21051 (#247) / Right Landing (#871)

| 字段 | 值 |
|---|---|
| SourceRegion | D21056 (#255) / Left Door (#925) |
| DestinationRegion | D21051 (#247) / Right Landing (#871) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2746 · 4 (#8) / Numa Door N (#178) / D1501 (#74) / Entrance Top Landing (#931)

| 字段 | 值 |
|---|---|
| SourceRegion | 4 (#8) / Numa Door N (#178) |
| DestinationRegion | D1501 (#74) / Entrance Top Landing (#931) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2747 · 4 (#8) / Numa Door E (#180) / D1501 (#74) / Entrance Right Landing (#933)

| 字段 | 值 |
|---|---|
| SourceRegion | 4 (#8) / Numa Door E (#180) |
| DestinationRegion | D1501 (#74) / Entrance Right Landing (#933) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2748 · 4 (#8) / Numa Door S (#182) / D1501 (#74) / Entrance Bottom Landing (#935)

| 字段 | 值 |
|---|---|
| SourceRegion | 4 (#8) / Numa Door S (#182) |
| DestinationRegion | D1501 (#74) / Entrance Bottom Landing (#935) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2749 · 4 (#8) / Numa Door W (#184) / D1501 (#74) / Entrance Left Landing (#937)

| 字段 | 值 |
|---|---|
| SourceRegion | 4 (#8) / Numa Door W (#184) |
| DestinationRegion | D1501 (#74) / Entrance Left Landing (#937) |
| Icon | Cave |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2750 · D1501 (#74) / Entrance Top Door (#930) / 4 (#8) / Numa Landing N (#179)

| 字段 | 值 |
|---|---|
| SourceRegion | D1501 (#74) / Entrance Top Door (#930) |
| DestinationRegion | 4 (#8) / Numa Landing N (#179) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2751 · D1501 (#74) / Entrance Right Door (#932) / 4 (#8) / Numa Landing E (#181)

| 字段 | 值 |
|---|---|
| SourceRegion | D1501 (#74) / Entrance Right Door (#932) |
| DestinationRegion | 4 (#8) / Numa Landing E (#181) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2752 · D1501 (#74) / Entrance Bottom Door (#934) / 4 (#8) / Numa Landing S (#183)

| 字段 | 值 |
|---|---|
| SourceRegion | D1501 (#74) / Entrance Bottom Door (#934) |
| DestinationRegion | 4 (#8) / Numa Landing S (#183) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2753 · D1501 (#74) / Entrance Left Door (#936) / 4 (#8) / Numa Landing W (#185)

| 字段 | 值 |
|---|---|
| SourceRegion | D1501 (#74) / Entrance Left Door (#936) |
| DestinationRegion | 4 (#8) / Numa Landing W (#185) |
| Icon | Exit |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2754 · D1501 (#74) / Floor 2 Door (#938) / D1502 (#75) / Floor 1 Landing (#942)

| 字段 | 值 |
|---|---|
| SourceRegion | D1501 (#74) / Floor 2 Door (#938) |
| DestinationRegion | D1502 (#75) / Floor 1 Landing (#942) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2755 · D1502 (#75) / Floor 1 Door (#941) / D1501 (#74) / Floor 2 Landing (#939)

| 字段 | 值 |
|---|---|
| SourceRegion | D1502 (#75) / Floor 1 Door (#941) |
| DestinationRegion | D1501 (#74) / Floor 2 Landing (#939) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2756 · D1502 (#75) / Floor 3 Top Door (#943) / D15032 (#77) / Top Landing (#959)

| 字段 | 值 |
|---|---|
| SourceRegion | D1502 (#75) / Floor 3 Top Door (#943) |
| DestinationRegion | D15032 (#77) / Top Landing (#959) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2757 · D1502 (#75) / Floor 3 Right Door (#945) / D15031 (#76) / Right Landing (#954)

| 字段 | 值 |
|---|---|
| SourceRegion | D1502 (#75) / Floor 3 Right Door (#945) |
| DestinationRegion | D15031 (#76) / Right Landing (#954) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2758 · D1502 (#75) / Floor 3 Bottom Door (#947) / D15034 (#79) / Bottom Landing (#969)

| 字段 | 值 |
|---|---|
| SourceRegion | D1502 (#75) / Floor 3 Bottom Door (#947) |
| DestinationRegion | D15034 (#79) / Bottom Landing (#969) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2759 · D1502 (#75) / Floor 3 Left Door (#949) / D15033 (#78) / Left Landing (#964)

| 字段 | 值 |
|---|---|
| SourceRegion | D1502 (#75) / Floor 3 Left Door (#949) |
| DestinationRegion | D15033 (#78) / Left Landing (#964) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2760 · D15032 (#77) / Top Floor 4 Door (#960) / D1504 (#80) / Floor 3 Top Landing (#1105)

| 字段 | 值 |
|---|---|
| SourceRegion | D15032 (#77) / Top Floor 4 Door (#960) |
| DestinationRegion | D1504 (#80) / Floor 3 Top Landing (#1105) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2761 · D15031 (#76) / Right Floor 4 Door (#955) / D1504 (#80) / Floor 3 Right Landing (#1106)

| 字段 | 值 |
|---|---|
| SourceRegion | D15031 (#76) / Right Floor 4 Door (#955) |
| DestinationRegion | D1504 (#80) / Floor 3 Right Landing (#1106) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2762 · D15034 (#79) / Bottom Floor 4 Door (#970) / D1504 (#80) / Floor 3 Bottom Landing (#1107)

| 字段 | 值 |
|---|---|
| SourceRegion | D15034 (#79) / Bottom Floor 4 Door (#970) |
| DestinationRegion | D1504 (#80) / Floor 3 Bottom Landing (#1107) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2763 · D15033 (#78) / Left Floor 4 Door (#965) / D1504 (#80) / Floor 3 Left Landing (#1108)

| 字段 | 值 |
|---|---|
| SourceRegion | D15033 (#78) / Left Floor 4 Door (#965) |
| DestinationRegion | D1504 (#80) / Floor 3 Left Landing (#1108) |
| Icon | Down |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2764 · D1504 (#80) / Floor 3 Top Door (#1101) / D15032 (#77) / Top Floor 4 Landing (#961)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Floor 3 Top Door (#1101) |
| DestinationRegion | D15032 (#77) / Top Floor 4 Landing (#961) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2765 · D1504 (#80) / Floor 3 Right Door (#1102) / D15031 (#76) / Right Floor 4 Landing (#956)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Floor 3 Right Door (#1102) |
| DestinationRegion | D15031 (#76) / Right Floor 4 Landing (#956) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2766 · D1504 (#80) / Floor 3 Bottom Door (#1103) / D15034 (#79) / Bottom Floor 4 Landing (#971)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Floor 3 Bottom Door (#1103) |
| DestinationRegion | D15034 (#79) / Bottom Floor 4 Landing (#971) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2767 · D1504 (#80) / Floor 3 Left Door (#1104) / D15033 (#78) / Left Floor 4 Landing (#966)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Floor 3 Left Door (#1104) |
| DestinationRegion | D15033 (#78) / Left Floor 4 Landing (#966) |
| Icon | Up |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2768 · D1504 (#80) / Fake Bottom Doors Top Area (#975) / D1504 (#80) / Top Landing Top Area (#978)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Fake Bottom Doors Top Area (#975) |
| DestinationRegion | D1504 (#80) / Top Landing Top Area (#978) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2769 · D1504 (#80) / Fake Top Doors Top Area (#976) / D1504 (#80) / Bottom Landing Top Area (#977)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Fake Top Doors Top Area (#976) |
| DestinationRegion | D1504 (#80) / Bottom Landing Top Area (#977) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2770 · D1504 (#80) / Fake Bottom Doors Left Area (#979) / D1504 (#80) / Top Landing Top Area (#978)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Fake Bottom Doors Left Area (#979) |
| DestinationRegion | D1504 (#80) / Top Landing Top Area (#978) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2771 · D1504 (#80) / Fake Top Doors  Left Area (#980) / D1504 (#80) / Bottom Landing Top Area (#977)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Fake Top Doors  Left Area (#980) |
| DestinationRegion | D1504 (#80) / Bottom Landing Top Area (#977) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2772 · D1504 (#80) / Fake Bottom Doors Right Area (#983) / D1504 (#80) / Top Landing  Left Area (#982)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Fake Bottom Doors Right Area (#983) |
| DestinationRegion | D1504 (#80) / Top Landing  Left Area (#982) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2773 · D1504 (#80) / Fake Top Doors  Right Area (#984) / D1504 (#80) / Bottom Landing  Left Area (#981)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Fake Top Doors  Right Area (#984) |
| DestinationRegion | D1504 (#80) / Bottom Landing  Left Area (#981) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2774 · D1504 (#80) / Real Bottom Door Top Aea (#987) / D1504 (#80) / Top Landing  Left Area (#982)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Real Bottom Door Top Aea (#987) |
| DestinationRegion | D1504 (#80) / Top Landing  Left Area (#982) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2775 · D1504 (#80) / Real Top Door Top Area (#988) / D1504 (#80) / Bottom Landing  Left Area (#981)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Real Top Door Top Area (#988) |
| DestinationRegion | D1504 (#80) / Bottom Landing  Left Area (#981) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2776 · D1504 (#80) / Real Bottom Door Left Area (#989) / D1504 (#80) / Top Landing  Right Area (#986)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Real Bottom Door Left Area (#989) |
| DestinationRegion | D1504 (#80) / Top Landing  Right Area (#986) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2777 · D1504 (#80) / Real Top Door  Left Area (#990) / D1504 (#80) / Bottom Landing  Right Area (#985)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Real Top Door  Left Area (#990) |
| DestinationRegion | D1504 (#80) / Bottom Landing  Right Area (#985) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2778 · D1504 (#80) / Real Bottom Door Right Area (#991) / D1505 (#81) / Row 1 Top Landing (#1012)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Real Bottom Door Right Area (#991) |
| DestinationRegion | D1505 (#81) / Row 1 Top Landing (#1012) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2779 · D1504 (#80) / Real Top Door  Right Area (#992) / D1505 (#81) / Row 1 Bottom Landing (#1011)

| 字段 | 值 |
|---|---|
| SourceRegion | D1504 (#80) / Real Top Door  Right Area (#992) |
| DestinationRegion | D1505 (#81) / Row 1 Bottom Landing (#1011) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2780 · D1505 (#81) / Row 1 Fake Bottom Doors (#1009) / D1505 (#81) / Row 1 Top Landing (#1012)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 1 Fake Bottom Doors (#1009) |
| DestinationRegion | D1505 (#81) / Row 1 Top Landing (#1012) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2781 · D1505 (#81) / Row 1 Fake Top Doors (#1010) / D1505 (#81) / Row 1 Bottom Landing (#1011)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 1 Fake Top Doors (#1010) |
| DestinationRegion | D1505 (#81) / Row 1 Bottom Landing (#1011) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2782 · D1505 (#81) / Row 1 Real Bottom Door (#1071) / D1505 (#81) / Row 2 Top Landing (#1022)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 1 Real Bottom Door (#1071) |
| DestinationRegion | D1505 (#81) / Row 2 Top Landing (#1022) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2783 · D1505 (#81) / Row 1 Real Top Door (#1072) / D1505 (#81) / Row 2 Bottom Landing (#1021)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 1 Real Top Door (#1072) |
| DestinationRegion | D1505 (#81) / Row 2 Bottom Landing (#1021) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2784 · D1505 (#81) / Row 2 Fake Bottom Doors (#1019) / D1505 (#81) / Row 1 Top Landing (#1012)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 2 Fake Bottom Doors (#1019) |
| DestinationRegion | D1505 (#81) / Row 1 Top Landing (#1012) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2785 · D1505 (#81) / Row 2 Fake Top Doors (#1020) / D1505 (#81) / Row 1 Bottom Landing (#1011)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 2 Fake Top Doors (#1020) |
| DestinationRegion | D1505 (#81) / Row 1 Bottom Landing (#1011) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2786 · D1505 (#81) / Row 2 Real Bottom door (#1073) / D1505 (#81) / Row 3 Top Landing (#1026)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 2 Real Bottom door (#1073) |
| DestinationRegion | D1505 (#81) / Row 3 Top Landing (#1026) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

### #2787 · D1505 (#81) / Row 2 Real Top door (#1074) / D1505 (#81) / Row 3 Bottom Landing (#1025)

| 字段 | 值 |
|---|---|
| SourceRegion | D1505 (#81) / Row 2 Real Top door (#1074) |
| DestinationRegion | D1505 (#81) / Row 3 Bottom Landing (#1025) |
| Icon | None |
| NeedHole | false |
| Effect | None |
| RequiredClass | All |
| SkipValidation | false |

