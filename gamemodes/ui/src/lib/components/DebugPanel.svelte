<script lang="ts">
  let isOpen = $state(false);
  let raceIntervalId = $state<ReturnType<typeof setInterval> | null>(null);
  let lobbyIntervalId = $state<ReturnType<typeof setInterval> | null>(null);
  let dmIntervalId = $state<ReturnType<typeof setInterval> | null>(null);
  let raceLapTime = $state(0);
  let lobbyRemaining = $state(0);
  let dmRemaining = $state(0);

  function dispatch(messageName: string, payload: unknown): void {
    window.postMessage(JSON.stringify({ messageName, payload }), '*');
  }

  function clearRaceInterval(): void {
    if (raceIntervalId !== null) {
      clearInterval(raceIntervalId);
      raceIntervalId = null;
    }
  }

  function clearLobbyInterval(): void {
    if (lobbyIntervalId !== null) {
      clearInterval(lobbyIntervalId);
      lobbyIntervalId = null;
    }
  }

  function clearDmInterval(): void {
    if (dmIntervalId !== null) {
      clearInterval(dmIntervalId);
      dmIntervalId = null;
    }
  }

  // -- Voting --

  function startVote(): void {
    dispatch('voting:start', {
      voteId: 'debug-vote-1',
      gameModes: [
        { id: 'race', name: 'Street Race', description: 'High-speed circuit racing through the city' },
        { id: 'dm', name: 'Deathmatch', description: 'Free-for-all vehicular combat' },
        { id: 'derby', name: 'Demolition Derby', description: 'Last car standing wins' },
      ],
      durationMs: 30000,
      metadata: { title: 'Vote for Next Game Mode', description: 'Choose what we play next' },
    });
  }

  function startVoteManyModes(): void {
    dispatch('voting:start', {
      voteId: 'debug-vote-2',
      gameModes: [
        { id: 'sprint', name: 'Highway Sprint', description: 'Point-to-point sprint across the map' },
        { id: 'race', name: 'Circuit Race', description: 'Multi-lap racing on closed circuits' },
        { id: 'dm', name: 'Team Deathmatch', description: '2v2v2 vehicular combat' },
        { id: 'derby', name: 'Demolition Derby', description: 'Last car standing wins' },
        { id: 'pursuit', name: 'Hot Pursuit', description: 'Cops vs racers chase mode' },
      ],
      durationMs: 15000,
    });
  }

  function completeVote(): void {
    dispatch('voting:completed', {
      voteId: 'debug-vote-1',
      winningGameModeId: 'race',
    });
  }

  function completeVoteNoWinner(): void {
    dispatch('voting:completed', {
      voteId: 'debug-vote-2',
      winningGameModeId: null,
    });
  }

  // -- Lobby --

  function startLobbyTimer(gameMode: string, duration: number, players: number): void {
    clearLobbyInterval();
    lobbyRemaining = duration;
    const send = () =>
      dispatch('lobby:hudUpdate', {
        isClientInLobbyMode: true,
        isGameModeActive: true,
        currentGameMode: gameMode,
        currentGameModeRemainingDuration: lobbyRemaining,
        currentGameModeNumberOfPlayers: players,
      });
    send();
    lobbyIntervalId = setInterval(() => {
      lobbyRemaining = Math.max(0, lobbyRemaining - 1);
      send();
      if (lobbyRemaining <= 0) clearLobbyInterval();
    }, 1000);
  }

  function showLobby(): void {
    startLobbyTimer('Street Race', 120, 8);
  }

  function showLobbyLowTime(): void {
    startLobbyTimer('Demolition Derby', 8, 3);
  }

  function showLobbyIdle(): void {
    clearLobbyInterval();
    dispatch('lobby:hudUpdate', {
      isClientInLobbyMode: true,
      isGameModeActive: false,
      currentGameMode: '',
      currentGameModeRemainingDuration: 0,
      currentGameModeNumberOfPlayers: 0,
    });
  }

  function hideLobby(): void {
    clearLobbyInterval();
    dispatch('lobby:hudUpdate', {
      isClientInLobbyMode: false,
      isGameModeActive: false,
      currentGameMode: '',
      currentGameModeRemainingDuration: 0,
      currentGameModeNumberOfPlayers: 0,
    });
  }

  // -- Race --

  const raceLeaderboard = [
    { name: 'SpeedDemon', score: 62 },
    { name: 'NitroKing', score: 65 },
    { name: 'DriftMaster', score: 68 },
    { name: 'TurboBlaze', score: 71 },
    { name: 'ApexHunter', score: 74 },
  ];

  function startRaceInterval(opts: {
    isSprint: boolean;
    currentLap: number;
    totalLaps: number;
    bestLapTimeSeconds: number;
    trackName: string;
    endTime: number | null;
  }): void {
    clearRaceInterval();
    raceLapTime = 0;
    const send = () =>
      dispatch('race:hudUpdate', {
        isActive: true,
        isDone: false,
        isSprint: opts.isSprint,
        currentLap: opts.currentLap,
        totalLaps: opts.totalLaps,
        lapTimeSeconds: raceLapTime,
        bestLapTimeSeconds: opts.bestLapTimeSeconds,
        trackName: opts.trackName,
        endTime: opts.endTime,
        leaderboard: raceLeaderboard.slice(0, 3),
      });
    send();
    raceIntervalId = setInterval(() => {
      raceLapTime += 1;
      send();
    }, 1000);
  }

  function raceLaps(): void {
    startRaceInterval({
      isSprint: false,
      currentLap: 2,
      totalLaps: 5,
      bestLapTimeSeconds: 58,
      trackName: 'Downtown Circuit',
      endTime: null,
    });
  }

  function raceLapsFirstLap(): void {
    startRaceInterval({
      isSprint: false,
      currentLap: 1,
      totalLaps: 5,
      bestLapTimeSeconds: 0,
      trackName: 'Mountain Pass',
      endTime: null,
    });
  }

  function raceSprint(): void {
    startRaceInterval({
      isSprint: true,
      currentLap: 1,
      totalLaps: 1,
      bestLapTimeSeconds: 0,
      trackName: 'Highway Sprint',
      endTime: null,
    });
  }

  function raceEnding(): void {
    clearRaceInterval();
    raceLapTime = raceLapTime || 45;
    let countdown = 10;
    dispatch('race:hudUpdate', {
      isActive: true,
      isDone: false,
      isSprint: false,
      currentLap: 4,
      totalLaps: 5,
      lapTimeSeconds: raceLapTime,
      bestLapTimeSeconds: 58,
      trackName: 'Downtown Circuit',
      endTime: countdown,
      leaderboard: raceLeaderboard.slice(0, 3),
    });
    raceIntervalId = setInterval(() => {
      raceLapTime += 1;
      countdown = Math.max(0, countdown - 1);
      dispatch('race:hudUpdate', {
        isActive: true,
        isDone: false,
        isSprint: false,
        currentLap: 4,
        totalLaps: 5,
        lapTimeSeconds: raceLapTime,
        bestLapTimeSeconds: 58,
        trackName: 'Downtown Circuit',
        endTime: countdown,
        leaderboard: raceLeaderboard.slice(0, 3),
      });
      if (countdown <= 0) clearRaceInterval();
    }, 1000);
  }

  function endRace(): void {
    clearRaceInterval();
    dispatch('race:hudUpdate', {
      isActive: true,
      isDone: true,
      isSprint: false,
      currentLap: 5,
      totalLaps: 5,
      lapTimeSeconds: raceLapTime || 312,
      bestLapTimeSeconds: 58,
      trackName: 'Downtown Circuit',
      endTime: 10,
      leaderboard: raceLeaderboard,
    });
  }

  function endRaceSprint(): void {
    clearRaceInterval();
    dispatch('race:hudUpdate', {
      isActive: true,
      isDone: true,
      isSprint: true,
      currentLap: 1,
      totalLaps: 1,
      lapTimeSeconds: raceLapTime || 127,
      bestLapTimeSeconds: 0,
      trackName: 'Highway Sprint',
      endTime: 10,
      leaderboard: raceLeaderboard,
    });
  }

  function clearRace(): void {
    clearRaceInterval();
    dispatch('race:hudUpdate', {
      isActive: false,
      isDone: false,
      isSprint: false,
      currentLap: 0,
      totalLaps: 0,
      lapTimeSeconds: 0,
      bestLapTimeSeconds: 0,
      trackName: '',
      endTime: null,
      leaderboard: [],
    });
  }

  // -- Deathmatch --

  const dmLeaderboard = [
    { name: 'FragMachine', score: 24 },
    { name: 'BulletStorm', score: 19 },
    { name: 'IronFist', score: 15 },
    { name: 'ShadowStrike', score: 12 },
    { name: 'VenomBlade', score: 8 },
  ];

  function dmCountdown(): void {
    clearDmInterval();
    let remaining = 5;
    dispatch('dm:hudUpdate', {
      matchState: 2,
      matchTypeName: 'Free For All',
      countdownRemainingSeconds: remaining,
      matchRemainingSeconds: 0,
      leaderboard: [],
      outsideArena: false,
      outsideArenaRemainingTime: 0,
    });
    dmIntervalId = setInterval(() => {
      remaining -= 1;
      dispatch('dm:hudUpdate', {
        matchState: 2,
        matchTypeName: 'Free For All',
        countdownRemainingSeconds: remaining,
        matchRemainingSeconds: 0,
        leaderboard: [],
        outsideArena: false,
        outsideArenaRemainingTime: 0,
      });
      if (remaining <= 0) clearDmInterval();
    }, 1000);
  }

  function dmActive(matchType: string, seconds: number): void {
    clearDmInterval();
    dmRemaining = seconds;
    const send = () =>
      dispatch('dm:hudUpdate', {
        matchState: 3,
        matchTypeName: matchType,
        countdownRemainingSeconds: 0,
        matchRemainingSeconds: dmRemaining,
        leaderboard: dmLeaderboard,
        outsideArena: false,
        outsideArenaRemainingTime: 0,
      });
    send();
    dmIntervalId = setInterval(() => {
      dmRemaining = Math.max(0, dmRemaining - 1);
      send();
      if (dmRemaining <= 0) clearDmInterval();
    }, 1000);
  }

  function dmActiveFFA(): void {
    dmActive('Free For All', 90);
  }

  function dmActiveLowTime(): void {
    dmActive('Free For All', 8);
  }

  function dmActiveTeam(): void {
    dmActive('Team Deathmatch', 120);
  }

  function dmOutsideArena(): void {
    clearDmInterval();
    let arenaTime = 10;
    dmRemaining = dmRemaining || 60;
    const send = () =>
      dispatch('dm:hudUpdate', {
        matchState: 3,
        matchTypeName: 'Free For All',
        countdownRemainingSeconds: 0,
        matchRemainingSeconds: dmRemaining,
        leaderboard: dmLeaderboard,
        outsideArena: true,
        outsideArenaRemainingTime: arenaTime,
      });
    send();
    dmIntervalId = setInterval(() => {
      arenaTime = Math.max(0, arenaTime - 1);
      dmRemaining = Math.max(0, dmRemaining - 1);
      send();
      if (arenaTime <= 0) clearDmInterval();
    }, 1000);
  }

  function dmEnding(): void {
    clearDmInterval();
    dispatch('dm:hudUpdate', {
      matchState: 4,
      matchTypeName: 'Free For All',
      countdownRemainingSeconds: 0,
      matchRemainingSeconds: 0,
      leaderboard: dmLeaderboard,
      outsideArena: false,
      outsideArenaRemainingTime: 0,
    });
  }

  // -- Chat --

  let chatMsgIndex = 0;
  const chatNames = ['SpeedDemon', 'NitroKing', 'DriftMaster', 'FragMachine', 'ShadowStrike'];
  const chatMessages = [
    'gg wp',
    'nice shot!',
    'anyone know the shortcut on this track?',
    'lol get rekt',
    'rematch?',
    'that was close',
  ];

  function chatSendMessage(): void {
    dispatch('chat:hudUpdate', {
      onlyOpen: false,
      author: chatNames[chatMsgIndex % chatNames.length],
      contents: chatMessages[chatMsgIndex % chatMessages.length],
    });
    chatMsgIndex++;
  }

  function chatOpenInput(): void {
    dispatch('chat:hudUpdate', {
      onlyOpen: true,
      author: null,
      contents: null,
    });
  }

  function dmClear(): void {
    clearDmInterval();
    dispatch('dm:hudUpdate', {
      matchState: 0,
      matchTypeName: '',
      countdownRemainingSeconds: 0,
      matchRemainingSeconds: 0,
      leaderboard: [],
      outsideArena: false,
      outsideArenaRemainingTime: 0,
    });
  }

  const btn = 'rounded-full px-2 py-0.5 text-[11px] font-medium text-white transition';
</script>

<div class="fixed top-4 left-4 z-[9999] select-none">
  <button
    class="rounded bg-gray-800/90 px-2 py-1 font-mono text-xs font-bold text-yellow-400 shadow-lg backdrop-blur transition hover:bg-gray-700/90"
    onclick={() => (isOpen = !isOpen)}
    aria-label="Toggle debug panel"
    aria-expanded={isOpen}
  >
    DBG
  </button>

  {#if isOpen}
    <div
      class="mt-2 max-h-[80vh] w-72 overflow-y-auto rounded-lg bg-gray-900/90 p-3 shadow-xl backdrop-blur-md"
      role="region"
      aria-label="Debug panel"
    >
      <!-- Voting -->
      <div class="mb-3">
        <p class="mb-1 text-[10px] font-semibold tracking-wider text-gray-400 uppercase">Voting</p>
        <div class="flex flex-wrap gap-1">
          <button class="{btn} bg-blue-600 hover:bg-blue-500" onclick={startVote}>Start Vote (3)</button>
          <button class="{btn} bg-blue-600 hover:bg-blue-500" onclick={startVoteManyModes}>Start Vote (5)</button>
          <button class="{btn} bg-blue-600 hover:bg-blue-500" onclick={completeVote}>Winner</button>
          <button class="{btn} bg-blue-600 hover:bg-blue-500" onclick={completeVoteNoWinner}>No Winner</button>
        </div>
      </div>

      <!-- Lobby -->
      <div class="mb-3">
        <p class="mb-1 text-[10px] font-semibold tracking-wider text-gray-400 uppercase">Lobby</p>
        <div class="flex flex-wrap gap-1">
          <button class="{btn} bg-green-600 hover:bg-green-500" onclick={showLobby}>Active (2m)</button>
          <button class="{btn} bg-green-600 hover:bg-green-500" onclick={showLobbyLowTime}>Low Time (8s)</button>
          <button class="{btn} bg-green-600 hover:bg-green-500" onclick={showLobbyIdle}>Idle</button>
          <button class="{btn} bg-green-600 hover:bg-green-500" onclick={hideLobby}>Hide</button>
        </div>
      </div>

      <!-- Race -->
      <div class="mb-3">
        <p class="mb-1 text-[10px] font-semibold tracking-wider text-gray-400 uppercase">Race</p>
        <div class="flex flex-wrap gap-1">
          <button class="{btn} bg-orange-600 hover:bg-orange-500" onclick={raceLaps}>Lap 2/5</button>
          <button class="{btn} bg-orange-600 hover:bg-orange-500" onclick={raceLapsFirstLap}>First Lap</button>
          <button class="{btn} bg-orange-600 hover:bg-orange-500" onclick={raceSprint}>Sprint</button>
          <button class="{btn} bg-orange-600 hover:bg-orange-500" onclick={raceEnding}>Ending</button>
          <button class="{btn} bg-orange-600 hover:bg-orange-500" onclick={endRace}>Results (Lap)</button>
          <button class="{btn} bg-orange-600 hover:bg-orange-500" onclick={endRaceSprint}>Results (Sprint)</button>
          <button class="{btn} bg-orange-600 hover:bg-orange-500" onclick={clearRace}>Clear</button>
        </div>
      </div>

      <!-- Chat -->
      <div class="mb-3">
        <p class="mb-1 text-[10px] font-semibold tracking-wider text-gray-400 uppercase">Chat</p>
        <div class="flex flex-wrap gap-1">
          <button class="{btn} bg-teal-600 hover:bg-teal-500" onclick={chatSendMessage}>Message</button>
          <button class="{btn} bg-teal-600 hover:bg-teal-500" onclick={chatOpenInput}>Open Input</button>
        </div>
      </div>

      <!-- Deathmatch -->
      <div>
        <p class="mb-1 text-[10px] font-semibold tracking-wider text-gray-400 uppercase">Deathmatch</p>
        <div class="flex flex-wrap gap-1">
          <button class="{btn} bg-red-600 hover:bg-red-500" onclick={dmCountdown}>Countdown</button>
          <button class="{btn} bg-red-600 hover:bg-red-500" onclick={dmActiveFFA}>FFA (90s)</button>
          <button class="{btn} bg-red-600 hover:bg-red-500" onclick={dmActiveLowTime}>FFA Low (8s)</button>
          <button class="{btn} bg-red-600 hover:bg-red-500" onclick={dmActiveTeam}>Team (2m)</button>
          <button class="{btn} bg-red-600 hover:bg-red-500" onclick={dmOutsideArena}>Outside Arena</button>
          <button class="{btn} bg-red-600 hover:bg-red-500" onclick={dmEnding}>Ending</button>
          <button class="{btn} bg-red-600 hover:bg-red-500" onclick={dmClear}>Clear</button>
        </div>
      </div>
    </div>
  {/if}
</div>
