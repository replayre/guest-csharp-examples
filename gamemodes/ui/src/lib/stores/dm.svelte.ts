import { type DmHudPayload } from '../types/events';

function createDmStore() {
  let state = $state<DmHudPayload | null>(null);

  return {
    get state() {
      return state;
    },

    update(payload: DmHudPayload) {
      state = payload;
    }
  };
}

export const dmStore = createDmStore();
