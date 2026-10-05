import { type LobbyHudPayload } from '../types/events';

function createLobbyStore() {
  let state = $state<LobbyHudPayload | null>(null);

  return {
    get state() {
      return state;
    },

    update(payload: LobbyHudPayload) {
      state = payload;
    }
  };
}

export const lobbyStore = createLobbyStore();
