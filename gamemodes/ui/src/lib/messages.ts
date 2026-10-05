type MessageHandler<T = unknown> = (payload: T) => void | Promise<void>;
type MessageHandlers = Map<string, MessageHandler>;

const handlers: MessageHandlers = new Map();

export function onMessage<T = unknown>(messageName: string, handler: MessageHandler<T>): void {
  handlers.set(messageName, handler as MessageHandler);
}

export function offMessage(messageName: string): void {
  handlers.delete(messageName);
}

export async function sendMessage<T = unknown>(
  messageName: string,
  payload?: T
): Promise<Response> {
  if (import.meta.env.DEV) {
    console.log(`[DEV] sendMessage → ${messageName}`, payload);
    return new Response(JSON.stringify({ ok: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  return fetch(`https://ugc-message/${messageName}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json; charset=UTF-8'
    },
    body: JSON.stringify(payload ?? {})
  });
}

window.addEventListener('message', (event) => {
  console.log('Message event received:', event);
  const { messageName, payload } = JSON.parse(event.data);
  console.log('Received message:', messageName, payload, event);
  if (!messageName) return;

  const handler = handlers.get(messageName);
  if (handler) {
    Promise.resolve()
      .then(() => handler(payload))
      .catch((error) => {
        console.error(`Error handling message "${messageName}":`, error);
      });
  }
});
