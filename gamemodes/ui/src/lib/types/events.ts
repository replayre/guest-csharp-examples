export interface VoteStartedPayload {
  voteId: string;
  gameModes: GameModeOption[];
  durationMs: number;
  metadata?: {
    title?: string;
    description?: string;
  };
}

export interface GameModeOption {
  id: string;
  name: string;
  description?: string;
  icon?: string;
}

export interface VoteCastPayload {
  voteId: string;
  gameModeId: string;
}

export interface VoteCompletedPayload {
  voteId: string;
  winningGameModeId: string | null;
}

export interface ChatHistoryEntry {
  author: string,
  contents: string;
}

export interface ChatStoreData {
  isChatWindowOpenForInput: boolean;
  isChatWindowOpenForHistory: boolean;
  history: ChatHistoryEntry[];
}

export interface ChatPayload {
  onlyOpen: boolean,
  author: string | null,
  contents: string | null;
}

export interface LobbyHudPayload {
  isClientInLobbyMode: boolean,
  isGameModeActive: boolean,
  isGameModeJoinable: boolean,
  currentGameMode: string;
  currentGameModeRemainingDuration: number;
  currentGameModeNumberOfPlayers: number;
}

export interface LeaderboardEntry {
  name: string;
  score: number | null;
}

export interface RaceHudPayload {
  isActive: boolean;
  isDone: boolean;
  isSprint: boolean;
  currentLap: number;
  totalLaps: number;
  lapTimeSeconds: number;
  bestLapTimeSeconds: number;
  trackName: string;
  endTime: number | null;
  leaderboard: LeaderboardEntry[];
}

export enum MatchState {
  Uninitialized,
  Starting,
  Countdown,
  Active,
  Ending,
  Ended,
}

export interface DmHudPayload {
  matchState: MatchState,
  matchTypeName: string;
  countdownRemainingSeconds: number;
  matchRemainingSeconds: number;
  leaderboard: LeaderboardEntry[];
  outsideArena: boolean;
  outsideArenaRemainingTime: number;
}

export interface RoamingHudPayload {
  isActive: boolean;
  isDead: boolean;
  isHelpScreenEnabled: boolean;
  isCharacterCustomizationAvailable: boolean;
  isVehicleSpawningAvailable: boolean;
  isCombatEnabled: boolean;
  timeUntilRespawn: number;
  timeUntilModeEnds: number;
  timeUntilVehicleSpawnCooldownEnds: number;
  timeUntilCombatToggleCooldownEnds: number;
}

export const MESSAGE_NAMES = {
  // Voting
  VOTING_START: 'voting:start',
  VOTING_CAST: 'voting:cast',
  VOTING_COMPLETED: 'voting:completed',

  // Chat
  CHAT_HUD_UPDATE: 'chat:hudUpdate',
  CHAT_PAYLOAD_SENT: 'chat:payloadSent',

  // Racing
  LOBBY_HUD_UPDATE: 'lobby:hudUpdate',

  // Racing
  RACE_HUD_UPDATE: 'race:hudUpdate',

  // DM
  DM_HUD_UPDATE: 'dm:hudUpdate',

  // Roaming
  ROAMING_HUD_UPDATE: 'roaming:hudUpdate',
} as const;
