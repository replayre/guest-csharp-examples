import { type RoamingHudPayload } from '../types/events';

function createRoamingStore() {
  let state = $state<RoamingHudPayload | null>(null);

  return {
    get state() {
      return state;
    },

    update(payload: RoamingHudPayload) {
      state = payload;
    }
  };
}

export const roamingStore = createRoamingStore();
