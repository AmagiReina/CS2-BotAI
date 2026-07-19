namespace BotAI;

internal static class LinuxPatchDefinitions
{
    internal static IReadOnlyDictionary<string, (string signature, string patch, string expectedOriginal, int patchOffset)> All { get; } =
        new Dictionary<string, (string signature, string patch, string expectedOriginal, int patchOffset)>()
        {
        // Confirmed against /home/misaka/cs2/game/csgo/bin/linuxsteamrt64/libserver.so (2026-06-09).
        // Force HasVisitedEnemySpawn = 1 so bots don't revisit enemy spawn.
        ["HasVisitedEnemySpawn"] = (
            signature:        "40 88 B7 6C 07 00 00 C3",
            patch:            "C6 87 6C 07 00 00 01",
            expectedOriginal: "40 88 B7 6C 07 00 00",
            patchOffset:      0
        ),

        // NOP the BombState reset in CSGameState::Reset() (linux-specific bytes).
        ["GameState_Reset"] = (
            signature:        "C6 47 08 00 48 89 07 48 C7 47 0C 00 00 00 00 C7 47 14 00 00 00 00 C3",
            patch:            "0F 1F 84 00 00 00 00 00",
            expectedOriginal: "48 C7 47 0C 00 00 00 00",
            patchOffset:      7
        ),

        // IdleState::OnUpdate: treat IsSafe() as false so bots don't idle near safe areas.
        ["Idle_IsSafeAlwaysFalse"] = (
            signature:        "48 89 DF E8 ? ? ? ? 84 C0 75 B3 48 8B 5D F8 C9 C3",
            patch:            "90 90",
            expectedOriginal: "75 B3",
            patchOffset:      10
        ),

        // EscapeFromBombState::OnEnter tail-call to EquipKnife() -> ret.
        // FIXED 2026-07-11 via binary diff against pre-update libserver.so:
        // struct field offset shifted -8 bytes (4F84 -> 4F7C), consistent with the
        // same recurring shift confirmed in ~10 other patches in this update.
        // The tail-call target ("E9", offset 12) is unaffected since it was already
        // wildcarded.
        ["EscapeFromBomb_OnEnter_NoEquipKnife"] = (
            signature:        "C6 83 7C 4F 00 00 00 48 8B 5D F8 C9 E9 ? ? ? ?",
            patch:            "C3 90 90 90 90",
            expectedOriginal: "E9 ? ? ? ?",
            patchOffset:      12
        ),

        // EscapeFromBombState::OnUpdate call to EquipKnife() -> NOP.
        ["EscapeFromBomb_OnUpdate_NoEquipKnife"] = (
            signature:        "48 85 C0 0F 84 ? ? ? ? 48 89 DF 49 89 C4 E8 ? ? ? ? 31 F6 48 89 DF E8 ? ? ? ?",
            patch:            "90 90 90 90 90",
            expectedOriginal: "E8 ? ? ? ?",
            patchOffset:      15
        ),

        // EscapeFromFlamesState::OnEnter call to EquipKnife() -> NOP.
        // FIXED 2026-07-11 via binary diff: two struct field offsets both shifted
        // -8 bytes (4F5C -> 4F54, 4F84 -> 4F7C), same recurring pattern.
        ["EscapeFromFlames_OnEnter_NoEquipKnife"] = (
            signature:        "C6 83 54 4F 00 00 00 48 89 DF C6 83 7C 4F 00 00 00 E8 ? ? ? ? F3 0F 10 1D",
            patch:            "90 90 90 90 90",
            expectedOriginal: "E8 ? ? ? ?",
            patchOffset:      17
        ),

        ["PlantBombLookAtPriorityLow"] = (
            signature:        "48 8D 55 C8 4C 89 E7 45 31 C9 F3 0F 10 40 08 45 31 C0 B9 02 00 00 00 48 89 5D C8 F3 0F 10 0D ? ? ? ? 48 8D 35 ? ? ? ? F3 0F 11 45 D0",
            patch:            "B9 00 00 00 00",
            expectedOriginal: "B9 02 00 00 00",
            patchOffset:      18
        ),

        ["DefuseBombLookAtPriorityLow"] = (
            signature:        "4C 89 E2 45 31 C9 45 31 C0 F3 0F 10 05 ? ? ? ? B9 02 00 00 00 48 89 DF 48 8D 35 ? ? ? ? E8 ? ? ? ?",
            patch:            "B9 00 00 00 00",
            expectedOriginal: "B9 02 00 00 00",
            patchOffset:      17
        ),

        // MoveToState::OnUpdate - DefuseBomb IsVisible gate removal.
        // FIXED 2026-07-11: recompiled stack-frame size shifted (48 83 C4 78 -> 68);
        // target jump ("0F 84" at offset 26) confirmed byte-identical otherwise.
        ["DefuseBomb_SkipIsVisibleCheck"] = (
            signature:        "0F 2F C8 0F 86 ? ? ? ? 31 C9 31 D2 4C 89 E6 48 89 DF E8 ? ? ? ? 84 C0 0F 84 ? ? ? ? 48 83 C4 68 48 89 DF",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      26
        ),

        // Skip fire-rate check in AttackState::Update for low-latency patch.
        // FIXED 2026-07-11 via binary diff against pre-update libserver.so: the
        // earlier candidates (0xC2B9EF/0x1A897E7) were both wrong - the real
        // function is at 0xA6283A. Confirmed by matching the surrounding trio of
        // near-identical comisd+jb checks byte-for-byte except for harmless
        // register-allocation differences (xmm1->xmm2 etc.); this check's own
        // register also changed (8B -> 93). Field offset (07A0) is unchanged since
        // it's a local stack slot, not a struct field.
        ["AttackState_SkipFireRateCheck"] = (
            signature:        "0F 2F 93 A0 07 00 00 0F 82 ? ? ? ?",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 82 ? ? ? ?",
            patchOffset:      7
        ),

        // AttackState::OnUpdate: keep the fire shortcut from returning early.
        ["AttackState_SkipSteadyFireShortcut"] = (
            signature:        "BA 01 00 00 00 48 89 DF 48 89 C6 E8 ? ? ? ? 84 C0 0F 84 ? ? ? ? 48 89 DF E8 ? ? ? ?",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      18
        ),

        // AttackState::OnUpdate: don't leave the zoom/lineup shortcut path early.
        ["AttackState_SkipZoomFireShortcut"] = (
            signature:        "F3 0F 10 05 ? ? ? ? 48 89 DF E8 ? ? ? ? 84 C0 0F 84 ? ? ? ? 83 BB C8 05 00 00 14",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      18
        ),

        // AttackState::OnUpdate: force the continuous-fire flag before SetLookAt().
        ["SprayAllDistances_ForceHoldTrigger"] = (
            signature:        "F3 0F 10 45 90 48 83 C4 68 4C 89 FE 48 89 DF 5B BA 01 00 00 00 41 5C 41 5D 41 5E 41 5F 5D E9 ? ? ? ?",
            patch:            "31 D2 90 90 90",
            expectedOriginal: "BA 01 00 00 00",
            patchOffset:      16
        ),

        // AttackState::OnEnter: always take the high-skill dodge chance path.
        // FIXED 2026-07-11: local stack-slot offset shifted (58 FE FF FF -> 48 FE FF FF);
        // "84 C0 0F 84" target sequence confirmed byte-identical otherwise.
        ["AttackState_DodgeChance100_Always"] = (
            signature:        "48 89 DF F3 0F 11 85 48 FE FF FF E8 ? ? ? ? 84 C0 0F 84 ? ? ? ? 44 8B 2D ? ? ? ? 45 89 EE",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      18
        ),

        // AttackState::OnUpdate: skip the CanSeeSniper retreat block.
        // FIXED 2026-07-11 via binary diff against pre-update libserver.so: the
        // near je (0F 84, 6 bytes) the compiler used before was re-encoded as a
        // short je (74 73, 2 bytes) since the branch target is now closer. The
        // fix mirrors that: same-size short-jmp (EB) instead of a 6-byte NOP+jmp.
        ["AttackState_RetreatOnSniper_Disable"] = (
            signature:        "48 8B 07 48 8D 15 ? ? ? ? 48 8B 80 38 05 00 00 48 39 D0 0F 85 ? ? ? ? 80 BF B8 05 00 00 00 74 73 4C 8D 35",
            patch:            "EB 73",
            expectedOriginal: "74 73",
            patchOffset:      33
        ),

        // AttackState::OnUpdate: don't leave the nearby-fire threat path because spread is zero.
        // FIXED 2026-07-11 via binary diff: the leading "48 89 DF; E8 <call>" prefix
        // this depended on no longer precedes the check (control flow reordered),
        // so the fix anchors on the stable remainder instead (float field load,
        // abs-compare, jbe) with its struct field offset shifted -8 bytes (E8 52 ->
        // E0 52), the same recurring shift seen elsewhere in this update.
        ["AttackState_SkipSniperSpreadCheck"] = (
            signature:        "F3 0F 10 8B E0 52 00 00 66 0F EF C0 0F 2F C8 0F 86 ? ? ? ?",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 86 ? ? ? ?",
            patchOffset:      15
        ),

        // Keep bot movement behavior when seeing enemies.
        ["AllSkill_KeepMoving_WhenSeeSniper"] = (
            signature:        "0F 2F 05 ? ? ? ? 76 0D 80 BB C4 05 00 00 00 0F 85",
            patch:            "90 90",
            expectedOriginal: "76 0D",
            patchOffset:      7
        ),

        // FIXED 2026-07-11: struct field offset shifted -8 bytes (5CA9 -> 5CA1,
        // consistent with a field removal earlier in CCSBot's layout - the same
        // -8 shift also shows up in Vision_AlwaysWatchApproachPoints below).
        // The je target itself (patchOffset 15) also naturally has a new
        // displacement (74 5F -> 74 5D) since that's a relative jump distance.
        ["AttackState_CanStrafe_jne"] = (
            signature:        "BE 01 00 00 00 48 89 DF E8 ? ? ? ? 84 C0 74 5D 80 BB A1 5C 00 00 00 0F 84",
            patch:            "90 90",
            expectedOriginal: "74 5D",
            patchOffset:      15
        ),

        // AttackState::OnEnter: force the reload-dodge chance flag true.
        // FIXED 2026-07-11: recompiled stack-frame size shifted (48 81 C4 98 01 00 00
        // -> A8 01 00 00); patch target (offset 8) precedes this and is unaffected.
        ["AttackState_DodgeDuringReload"] = (
            signature:        "F3 0F 59 40 08 0F 2F C8 41 0F 97 44 24 44 48 81 C4 A8 01 00 00",
            patch:            "41 C6 44 24 44 01",
            expectedOriginal: "41 0F 97 44 24 44",
            patchOffset:      8
        ),

        // AttackState::OnEnter: force the crouch-dodge chance flag true.
        ["SniperCrouchDodge_jb"] = (
            signature:        "0F 2F F8 66 0F EF C0 41 0F 93 44 24 42 E8 ? ? ? ? 48 8B 43 08",
            patch:            "41 C6 44 24 42 01",
            expectedOriginal: "41 0F 93 44 24 42",
            patchOffset:      7
        ),

        // AttackState::OnEnter: don't require the current weapon to be a sniper for dodge A.
        ["SniperDodge_SkipIsSniper_DodgeA"] = (
            signature:        "48 89 DF E8 ? ? ? ? 84 C0 0F 84 ? ? ? ? 44 8B 35 ? ? ? ? F3 0F 10 0D",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      10
        ),

        // Keep low-skill bots from delaying dodge transitions.
        ["LowSKill_JumpChance0"] = (
            signature:        "4C 0F 2F 05 ? ? ? ? 76 11",
            patch:            "EB 40",
            expectedOriginal: "76 11",
            patchOffset:      8
        ),

        // CCSBot::UpdateLookAround: ignore the movement timer gate.
        // FIXED 2026-07-11: cross-checking against the Windows dictionary entry
        // for this same patch (comiss; ja <patch target>; mov;mov;call - all
        // adjacent, no split) revealed I had the wrong location. 0xBF6C80 (the
        // "cmp qword[rbx+0x5550]" chain) was a red herring - a near-exact match,
        // but for unrelated, compiler-folded bail-out code. The real gate is at
        // 0xC8E5D3, which I'd actually found early in this session and wrongly
        // discarded: movss[rbx+0x600] (unique in the whole binary) -> pxor xmm1,xmm1
        // -> comiss xmm0,xmm1 -> jp/je (both already skip forward to the
        // continuation, i.e. NaN and ==0 already fall through as intended) -> ja
        // (the actual gate; NEW build re-encoded it from a short "77" to a near
        // "0F 87" and added the NaN-safety jp/je pair, but the semantics are
        // unchanged from the original OLD-build comiss+ja shape). NOPing the ja
        // makes every case fall through to the continuation, matching what the
        // patch always intended.
        ["Vision_SkipIsMovingGate"] = (
            signature:        "F3 0F 10 83 00 06 00 00 66 0F EF C9 0F 2F C1 7A 08 74 06 0F 87 ? ? ? ?",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 87 ? ? ? ?",
            patchOffset:      19
        ),

        // --- Vision_AlwaysEnterApproachBody: REAL FIX 2026-07-13 ---
        // Root cause (confirmed via disassembly): forcing entry unconditionally
        // lands on "mov rdi,[rbx+0x18]; call 0xD2EA10" where the callee immediately
        // does "mov rbx,[rdi+0x528]" with no null check. [rbx+0x18] is only valid
        // when the byte flag this patch bypasses is set - when it's null (no
        // current approach point, e.g. right after spawn/death), that's a null
        // pointer dereference -> SIGSEGV. Matches the report exactly (worse with
        // more bots, worst right after kills).
        // A same-size opcode flip can't add a real check, so this uses a genuine
        // code-cave: a 22-byte block of unused int3 padding at 0xBF686A (right
        // after an unrelated function's "jmp 0xbf67b2" epilogue, verified unique).
        // The cave does the null-check itself:
        //   cmp qword ptr [rbx+0x18], 0
        //   je  <Y>   ; original "no approach point" path - same target the
        //               unpatched jne would have gone to when the flag was clear
        //   jmp <X>   ; original "has approach point" path - same target this
        //               patch always intended to force
        // i.e. this restores an equivalent safety gate (on pointer validity
        // instead of the byte flag - they should track each other), while still
        // always taking the "has approach point" branch whenever it's genuinely
        // safe to. This entry MUST be applied before the redirect entry below
        // (dictionary insertion order - .NET Dictionary preserves it here since
        // nothing is ever removed).
        ["Vision_AlwaysEnterApproachBody_Cave"] = (
            signature:        "48 89 DF FF D0 E9 48 FF FF FF CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC",
            patch:            "48 83 7B 18 00 0F 84 13 01 00 00 E9 50 01 00 00",
            expectedOriginal: "CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC",
            patchOffset:      10
        ),

        // Always take approach-body path in Vision logic, but only when it's
        // actually safe to (see cave entry above). Redirects the original jne
        // site into the cave instead of converting it to an unconditional jmp.
        ["Vision_AlwaysEnterApproachBody"] = (
            signature:        "80 BB 39 04 00 00 00 0F 85 ? ? ? ? E9 ? ? ? ? 66 0F 1F 44 00 00 48 89 DF",
            patch:            "E9 0E F7 FF FF 90",
            expectedOriginal: "0F 85 ? ? ? ?",
            patchOffset:      7
        ),

        // --- Vision_AlwaysWatchApproachPoints: REAL FIX 2026-07-13 ---
        // My "RE-ENABLED" note above was wrong - re-traced this properly after
        // it was reported crashing (thank you for catching that; the count-gate
        // I pointed to doesn't actually protect the path I assumed).
        // The loop this forces entry into is do-while shaped: "jmp 0xbf6b4e"
        // unconditionally runs the body ONCE before the count check
        // (edx=[rbx+0x54f0] vs loop index) is ever reached. That first pass does
        // "mov rdx,[rbx+0x18]; ...; cmp byte[rdx+0x624],2" with no null check -
        // same risky field as Vision_AlwaysEnterApproachBody, same root cause
        // class. In STOCK code the outer je (which I'd NOP'd) is the ONLY thing
        // that ever prevented this when the approach-point count is 0 - so this
        // crashes whenever a bot has no current enemy target, independent of how
        // many approach points it knows about. Matches the report: worse with
        // more bots, worse right after kills (a bot's "current enemy" pointer
        // clears when its target dies).
        // Same fix pattern as before - a genuine code cave with a null check,
        // this time using 22 bytes of unused int3 padding at 0xC0CAAA (a
        // different, verified-unique location from the other patch's cave, since
        // that one's already fully used).
        ["Vision_AlwaysWatchApproachPoints_Cave"] = (
            signature:        "F3 0F 11 4D A8 E9 CD FE FF FF CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC",
            patch:            "48 83 7B 18 00 0F 84 71 A1 FE FF E9 11 A0 FE FF",
            expectedOriginal: "CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC",
            patchOffset:      10
        ),

        // CCSBot::UpdateLookAround: run the approach-point watch loop whenever present,
        // but only when [rbx+0x18] is actually valid (see cave entry above).
        // Redirects the original je site into the cave instead of NOPing it.
        ["Vision_AlwaysWatchApproachPoints"] = (
            signature:        "F3 0F 58 85 EC FE FF FF 80 BB F0 54 00 00 00 F3 0F 11 83 28 53 00 00 0F 84 ? ? ? ? F3 0F 10 1D",
            patch:            "E9 E0 5F 01 00 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      23
        ),

        // --- Vision_AlwaysWatchApproachPoints: SECOND FIX 2026-07-14 ---
        // Still crashed with the cave above in place. Traced further: even with
        // [rbx+0x18] guaranteed valid, the loop body itself has an UNRELATED
        // unguarded pointer - "mov rdi,[r15+0x10]" (a per-entry field in the
        // approach-points array, r15 walking the array) is passed straight into
        // "call 0xd05ce0" with zero null check, either at the call site or inside
        // the callee itself (which does "movss xmm0,[rdi+rsi*4+0xc0]" immediately,
        // no test at all). Any approach-point slot with an unset/invalid entry
        // pointer crashes here - a completely separate risk from the entry gate,
        // and one that exists on EVERY loop iteration, not just entry into the
        // function. This is likely the actual differential cause between "only
        // Vision_AlwaysEnterApproachBody" (fine) and "both enabled" (crashes),
        // since this loop only runs at all because of this patch.
        // Fix: redirect the load+compare (11 bytes: "mov rdi,[r15+0x10]" +
        // "cmp byte[rdx+0x624],2") into a 32-byte cave at 0xC13E81 that re-does
        // the load, null-checks rdi, skips this entry entirely (jumps to the next
        // iteration, 0xbf6b08) if invalid, and otherwise replicates the original
        // compare/branch/call-setup exactly before resuming normal flow at the
        // original call instruction.
        ["Vision_AlwaysWatchApproachPoints_LoopEntry_Cave"] = (
            signature:        "48 8B 07 FF 50 20 E9 26 FF FF FF CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC",
            patch:            "49 8B 7F 10 48 85 FF 0F 84 7A 2C FE FF 80 BA 24 06 00 00 02 75 05 BE 03 00 00 00 E9 C8 2C FE FF",
            expectedOriginal: "CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC",
            patchOffset:      11
        ),

        ["Vision_AlwaysWatchApproachPoints_LoopEntry"] = (
            signature:        "49 8B 56 18 BE 02 00 00 00 49 8B 7F 10 80 BA 24 06 00 00 02",
            patch:            "E9 25 D3 01 00 90 90 90 90 90 90",
            expectedOriginal: "49 8B 7F 10 80 BA 24 06 00 00 02",
            patchOffset:      9
        ),
        // CCSBot::UpdateLookAround: skip the skill threshold before approach-body checks.
        // FIXED 2026-07-11 via binary diff: the short jbe (76 6D, 2 bytes) got
        // re-encoded as a near jbe (0F 86, 6 bytes) since the branch target is now
        // farther away, and the struct field offset shifted -8 bytes (5978 -> 5970).
        ["Vision_ApproachBody_SkipSkillCheck"] = (
            signature:        "F3 0F 10 40 0C 0F 2F 05 ? ? ? ? 0F 86 ? ? ? ? F3 0F 10 83 70 59 00 00",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 86 ? ? ? ?",
            patchOffset:      12
        ),

        // CCSBot::UpdateLookAround: don't leave the approach-body path when the hiding spot cone check fails.
        // FIXED 2026-07-11 via binary diff: found the real call site (0xBF7110) by
        // tracing the call target instead of matching bytes directly - it calls
        // straight into the InViewCone function confirmed earlier in this session
        // (0xBD6E60, the true entry point 9 bytes before where the InViewCone AOB
        // signature matches inside its body). Struct field offset (0xB0) is
        // unchanged here. The 2-byte short je got re-encoded as a 6-byte near je
        // (0F 84), same pattern seen in several other patches this session.
        ["Vision_ApproachBody_SkipHidingSpotCheck"] = (
            signature:        "48 89 DF E8 ? ? ? ? 85 C0 0F 84 ? ? ? ? 48 8B 03 48 8D 15 ? ? ? ? 48 8B 80 B0 00 00 00",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      10
        ),

        // Keep this entry as a Linux no-op for now. The Windows patch targets a noticable
        // helper, but current Linux builds inline that path into CCSBot::IsVisible(player).
        // Returning true from the function entry causes wall visibility; jumping over the
        // inline gate crashes when players connect on the current server build.
        ["IsNoticable_AlwaysTrue"] = (
            signature:        "55 48 8D 05 ? ? ? ? 48 89 E5 41 57 4C 8D 7D B0 41 56 41 89 D6 41 55 4C 89 FA",
            patch:            "55 48 8D",
            expectedOriginal: "55 48 8D",
            patchOffset:      0
        ),

        // CCSBot::InViewCone: do not reject targets outside the 60-degree outer FOV.
        // FIXED 2026-07-11: struct field offset shifted (0D18 -> 0D48) and vtable
        // slot shifted +0x18/24 bytes = 3 new vtable entries (D0 -> E8), consistent
        // with vtable growth elsewhere from this engine update. The final "77 20"
        // jump (patch target, offset 32) is confirmed byte-identical.
        ["InViewCone_RemoveOuterFOV"] = (
            signature:        "48 8B 47 18 48 8B 98 48 0D 00 00 48 8B 03 48 89 DF FF 90 E8 00 00 00 31 C0 0F 2F 05 ? ? ? ? 77 20",
            patch:            "90 90",
            expectedOriginal: "77 20",
            patchOffset:      32
        ),

        // CCSBot::InViewCone: treat all accepted targets as inside the inner cone.
        // FIXED 2026-07-11: same vtable-slot shift as InViewCone_RemoveOuterFOV
        // above (D0 -> E8, both calls resolve through the same vtable). With that
        // corrected, the rest of the signature matches byte-for-byte at 0xBD6E91,
        // immediately following the OuterFOV function - confirms this is the
        // right location.
        ["InViewCone_RemoveInnerFOV"] = (
            signature:        "FF 90 E8 00 00 00 B8 01 00 00 00 BA 02 00 00 00 0F 2F 05 ? ? ? ? 0F 46 C2 48 8B 5D F8 C9 C3",
            patch:            "89 D0 90",
            expectedOriginal: "0F 46 C2",
            patchOffset:      23
        ),

        // Keep InvestigateNoise always open in the same way as Windows.
        ["InvestigateNoise_SkipSelfDefenseCheck"] = (
            signature:        "83 BB ? ? 00 00 02 74 1E",
            patch:            "90 90",
            expectedOriginal: "74 1E",
            patchOffset:      7
        ),

        // CCSBot::OnAudibleEvent: accept sounds regardless of distance.
        // FIXED 2026-07-11: register allocation and local stack-slot changed
        // (xmm7 -> xmm5, stack offset -0xF0 -> -0xD4) - harmless recompile churn.
        // The "0F 86" target jump (patch site, offset 15) is confirmed unchanged.
        ["OnAudibleEvent_GlobalHearRange"] = (
            signature:        "F3 0F 51 ED 0F 2F E5 F3 0F 11 AD 2C FF FF FF 0F 86 ? ? ? ? 4C 89 EF",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 86 ? ? ? ?",
            patchOffset:      15
        ),

        // Idle/bomb-search fallback: GetNextBombsiteToSearch() -> GetPlantedBombsite().
        // FIXED 2026-07-11 via binary diff against pre-update libserver.so: struct
        // field offset shifted -8 bytes (5E10 -> 5E08). More importantly, the
        // patch redirects this call to GetPlantedBombsite() via a hardcoded
        // relative-call displacement, which is inherently build-specific -
        // recomputed here by locating GetPlantedBombsite's actual body in the new
        // binary (verified via a 64-byte exact match at 0xBE7A90) and calculating
        // the new displacement from this call site to it.
        ["TBot_BombsiteSearch_UseKnownPlantedSite"] = (
            signature:        "48 8B BB 08 5E 00 00 E8 ? ? ? ? 4C 89 F7 E8 ? ? ? ? 49 8B 3C 24 31 F6",
            patch:            "E8 6C 1D F6 FF",
            expectedOriginal: "E8 5C 20 F6 FF",
            patchOffset:      15
        ),

        // OnBombPickedUp: force the pathfind/hear gate to enter the tracking path.
        ["BombPickup_CT_GlobalHearRange"] = (
            signature:        "E8 ? ? ? ? 31 C9 BA 02 00 00 00 48 89 DF F3 0F 10 05 ? ? ? ? 48 89 C6 E8 ? ? ? ? 84 C0 75 84",
            patch:            "EB 84",
            expectedOriginal: "75 84",
            patchOffset:      33
        ),

        // OnBombBeep: ignore the 1500-unit hear range and update the bombsite from any distance.
        ["BombBeep_CT_GlobalHearRange"] = (
            signature:        "F3 0F 58 C2 F3 0F 58 C1 F3 0F 10 0D ? ? ? ? 0F 2F C8 0F 86 ? ? ? ? 48 8B 43 18",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 86 ? ? ? ?",
            patchOffset:      19
        ),

        // CSGameState::OnBombPlanted: all bot-owned game states learn the planted site.
        // FIXED 2026-07-11 via binary diff against pre-update libserver.so: struct
        // field offset shifted -8 bytes (5108 -> 5100), the same recurring pattern
        // confirmed in ~10 other patches in this update. Rest of the sequence,
        // including the "0F 84" jump target, matches exactly at 0xC0E9C9; only the
        // jump's own displacement differs as expected (10 01 00 00 -> EF 00 00 00),
        // and the jmp-conversion patch bytes are recomputed accordingly
        // (jmp_disp = je_disp + 1, to account for the 1-byte-shorter encoding).
        ["OnBombPlanted_AllBotsLearnSite"] = (
            signature:        "48 8B 83 00 51 00 00 48 8B 40 18 80 B8 24 06 00 00 02 0F 84 EF 00 00 00 48 8B 7B 18",
            patch:            "E9 F0 00 00 00 90",
            expectedOriginal: "0F 84 EF 00 00 00",
            patchOffset:      18
        ),

        // CT defuse task path: SetDisposition(SELF_DEFENSE) -> ENGAGE_AND_INVESTIGATE.
        // VERIFIED 2026-07-11: still matches the current binary byte-for-byte
        // (unique match at 0xC85497). No change needed.
        ["CT_Defuse_EngageAndInvestigate"] = (
            signature:        "48 8B 05 ? ? ? ? BE 02 00 00 00 48 89 DF 48 89 83 C8 05 00 00 E8 ? ? ? ? BA 02 00 00 00 4C 89 EE E9 ? ? ? ?",
            patch:            "BE 00 00 00 00",
            expectedOriginal: "BE 02 00 00 00",
            patchOffset:      7
        ),

        // DefuseBombState::OnUpdate: SetDisposition(SELF_DEFENSE) -> ENGAGE_AND_INVESTIGATE.
        // FIXED 2026-07-11: struct field offset shifted -8 bytes (5108 -> 5100),
        // same pattern seen repeatedly elsewhere in this update. Rest of the
        // sequence (including the "BE 02 00 00 00" patch target) matches exactly.
        ["DefuseBombState_OnUpdate_EngageAndInvestigate"] = (
            signature:        "55 48 8D BE 00 51 00 00 48 89 E5 41 54 53 48 89 F3 E8 ? ? ? ? BE 02 00 00 00 48 89 DF 49 89 C4 E8 ? ? ? ?",
            patch:            "BE 00 00 00 00",
            expectedOriginal: "BE 02 00 00 00",
            patchOffset:      22
        ),

        // DefuseBombState::OnEnter: SetDisposition(SELF_DEFENSE) -> ENGAGE_AND_INVESTIGATE.
        // FIXED 2026-07-11: struct field offset shifted -8 bytes (5E10 -> 5E08),
        // same pattern as above. Rest of the sequence matches exactly.
        ["DefuseBombState_OnEnter_EngageAndInvestigate"] = (
            signature:        "55 48 89 E5 41 54 53 48 89 F3 BE 02 00 00 00 48 89 DF E8 ? ? ? ? 4C 8B A3 08 5E 00 00",
            patch:            "BE 00 00 00 00",
            expectedOriginal: "BE 02 00 00 00",
            patchOffset:      10
        ),

        // Disable flashbang avoidance SetLookAt/StopAiming block.
        // FIXED 2026-07-11: two struct field offsets both shifted -8 bytes
        // (5368 -> 5360, 5C5C -> 5C54) - the same recurring shift confirmed in
        // four other Linux patches in this update. Everything else, including
        // both wildcarded rip-relative loads, matches exactly at 0xC0BAE2.
        ["FlashbangAvoidance_Disable"] = (
            signature:        "48 8D 35 ? ? ? ? 4C 89 E7 49 C7 84 24 60 53 00 00 00 00 00 00 0F 5C C2 F3 0F 11 4D A0 F3 0F 10 0D ? ? ? ? 0F 13 45 98 F3 0F 10 05 ? ? ? ? E8 ? ? ? ? 41 C6 84 24 54 5C 00 00 00",
            patch:            "90 90 90 90 90 90 90 90 90 90 90 90 90 90",
            expectedOriginal: "E8 ? ? ? ? 41 C6 84 24 54 5C 00 00 00",
            patchOffset:      50
        )
    };
}
