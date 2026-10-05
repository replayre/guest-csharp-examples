import type { RaceHudPayload } from '../types/events';

function createRaceStore() {
  let state = $state<RaceHudPayload | null>(null);

  return {
    get state() {
      return state;
    },

    get isActive() {
      return state?.isActive ?? false;
    },

    update(payload: RaceHudPayload) {
      state = payload.isActive ? payload : null;
    }
  };
}

export const raceStore = createRaceStore();
