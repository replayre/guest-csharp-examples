<script lang="ts">
  import './layout.css';
  import {
    registerGameModeHandlers,
    VotingOverlay,
    LobbyHud,
    RaceHud,
    DmHud,
    RoamingHud
  } from '$lib';
  import ChatOverlay from '$lib/components/ChatOverlay.svelte';
  import { onMount } from 'svelte';
  import { registerServiceWorker } from '$lib/register';
  import DebugPanel from '$lib/components/DebugPanel.svelte';

  const isDev = import.meta.env.DEV;

  onMount(() => {
    console.log('[Layout] Registering game mode handlers');
    registerGameModeHandlers();
    registerServiceWorker();
  });

  let { children } = $props();
</script>

<VotingOverlay />
<LobbyHud />
<RaceHud />
<DmHud />
<RoamingHud />
<ChatOverlay />

{#if isDev}
  <DebugPanel />
{/if}

{@render children()}
