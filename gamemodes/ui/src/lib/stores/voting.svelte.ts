import type { VoteStartedPayload, VoteCompletedPayload } from '../types/events';
import { MESSAGE_NAMES } from '../types/events';
import { sendMessage } from '../messages';

function createVotingStore() {
  let voteId = $state<string | null>(null);
  let gameModes = $state<VoteStartedPayload['gameModes']>([]);
  let selectedGameModeId = $state<string | null>(null);
  let durationMs = $state(0);
  let startTimeMs = $state(0);
  let isActive = $state(false);
  let isCompleted = $state(false);
  let winningGameModeId = $state<string | null>(null);
  let now = $state(Date.now());

  // Start ticker
  const interval = setInterval(() => {
    now = Date.now();
  }, 100);

  return {
    get voteId() { return voteId; },
    get gameModes() { return gameModes; },
    get selectedGameModeId() { return selectedGameModeId; },
    get durationMs() { return durationMs; },
    get startTimeMs() { return startTimeMs; },
    get isActive() { return isActive; },
    get isCompleted() { return isCompleted; },
    get winningGameModeId() { return winningGameModeId; },

    get elapsedMs() {
      if (!isActive) return 0;
      return Math.max(0, now - startTimeMs);
    },

    get remainingMs() {
      if (!isActive) return 0;
      const elapsed = Math.max(0, now - startTimeMs);
      return Math.max(0, durationMs - elapsed);
    },

    get timerExpired() {
      const remaining = isActive ? Math.max(0, durationMs - Math.max(0, now - startTimeMs)) : 0;
      return remaining <= 0;
    },

    startVoting(payload: VoteStartedPayload) {
      console.log('[VotingStore] startVoting called:', payload);
      if (isActive) {
        console.warn(
          `[Voting] New vote started (ID: ${payload.voteId}) while previous vote (ID: ${voteId}) was in progress. Replacing.`
        );
      }
      voteId = payload.voteId;
      gameModes = payload.gameModes;
      selectedGameModeId = null;
      durationMs = payload.durationMs;
      startTimeMs = Date.now();
      isActive = true;
      isCompleted = false;
      winningGameModeId = null;
      console.log('[VotingStore] State after startVoting:', { voteId, gameModes, isActive, durationMs });
    },

    selectGameMode(gameModeId: string) {
      if (!isActive || isCompleted) {
        console.warn('[Voting] Attempted to vote when voting is not active');
        return;
      }

      if (!gameModes.find((gm) => gm.id === gameModeId)) {
        console.error(`[Voting] Unknown game mode: ${gameModeId}`);
        return;
      }

      selectedGameModeId = gameModeId;
    },

    completeVoting(payload: VoteCompletedPayload) {
      if (voteId !== payload.voteId) {
        console.warn(
          `[Voting] Received completion for different vote ID. Current: ${voteId}, Received: ${payload.voteId}`
        );
        return;
      }

      isActive = false;
      isCompleted = true;
      winningGameModeId = payload.winningGameModeId;
    },

    reset() {
      voteId = null;
      gameModes = [];
      selectedGameModeId = null;
      durationMs = 0;
      startTimeMs = 0;
      isActive = false;
      isCompleted = false;
      winningGameModeId = null;
    },

    destroy() {
      clearInterval(interval);
    }
  };
}

export const votingStore = createVotingStore();

export async function submitVote() {
  if (!votingStore.voteId || !votingStore.selectedGameModeId) {
    throw new Error('No active vote or game mode selected');
  }

  const voteData = {
    voteId: votingStore.voteId,
    gameModeId: votingStore.selectedGameModeId
  };

  return sendMessage(MESSAGE_NAMES.VOTING_CAST, voteData);
}
