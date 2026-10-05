import { type ChatStoreData, type ChatPayload } from '../types/events';
import { MESSAGE_NAMES } from '../types/events';
import { sendMessage } from '../messages';

function createChatStore() {
  let state = $state<ChatStoreData>({ isChatWindowOpenForInput: false, isChatWindowOpenForHistory: false, history: [] });
  let closeTimer: ReturnType<typeof setTimeout> | null = null;

  const CLOSE_DELAY = 5000;

  return {
    get state() {
      return state;
    },

    update(payload: ChatPayload) {
      if (payload.onlyOpen) {
        state.isChatWindowOpenForInput = true;
      } else if (payload.author && payload.contents) {
        state.history.push({ author: payload.author, contents: payload.contents });
        state.isChatWindowOpenForHistory = true;

        if (closeTimer) {
          clearTimeout(closeTimer);
        }

        closeTimer = setTimeout(() => {
          state.isChatWindowOpenForHistory = false;
        }, CLOSE_DELAY);
      }
    }
  };
}

export const chatStore = createChatStore();

export async function submitChatMessage(contents: string | null, releaseFocus: boolean = true) {
  const payloadData = {
    contents: contents ?? '',
    releaseFocus,
  };

  await sendMessage(MESSAGE_NAMES.CHAT_PAYLOAD_SENT, payloadData);
  chatStore.state.isChatWindowOpenForInput = false;
}
