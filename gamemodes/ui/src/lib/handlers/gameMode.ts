import { onMessage } from '../messages';
import { votingStore } from '../stores/voting.svelte';
import { lobbyStore } from '../stores/lobby.svelte';
import { raceStore } from '../stores/race.svelte';
import { dmStore } from '../stores/dm.svelte';
import { roamingStore } from '../stores/roaming.svelte';
import { MESSAGE_NAMES } from '../types/events';
import type {
  VoteStartedPayload,
  VoteCompletedPayload,
  RaceHudPayload,
  DmHudPayload,
  LobbyHudPayload,
  RoamingHudPayload,
  ChatPayload
} from '../types/events';
import { chatStore } from '$lib/stores/chat.svelte';

export function registerGameModeHandlers(): void {
  onMessage<VoteStartedPayload>(MESSAGE_NAMES.VOTING_START, (payload) => {
    console.log('[GameMode] Voting started:', {
      voteId: payload.voteId,
      modes: payload.gameModes.length,
      durationMs: payload.durationMs
    });
    votingStore.startVoting(payload);
  });

  onMessage<VoteCompletedPayload>(MESSAGE_NAMES.VOTING_COMPLETED, (payload) => {
    console.log('[GameMode] Voting completed:', {
      voteId: payload.voteId,
      winner: payload.winningGameModeId
    });
    votingStore.completeVoting(payload);
  });

  onMessage<ChatPayload>(MESSAGE_NAMES.CHAT_HUD_UPDATE, (payload) => {
    chatStore.update(payload);
  });

  onMessage<LobbyHudPayload>(MESSAGE_NAMES.LOBBY_HUD_UPDATE, (payload) => {
    lobbyStore.update(payload);
  });

  onMessage<RaceHudPayload>(MESSAGE_NAMES.RACE_HUD_UPDATE, (payload) => {
    raceStore.update(payload);
  });

  onMessage<DmHudPayload>(MESSAGE_NAMES.DM_HUD_UPDATE, (payload) => {
    dmStore.update(payload);
  });

  onMessage<RoamingHudPayload>(MESSAGE_NAMES.ROAMING_HUD_UPDATE, (payload) => {
    roamingStore.update(payload);
  });
}
