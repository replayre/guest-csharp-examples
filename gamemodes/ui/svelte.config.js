import adapter from '@sveltejs/adapter-static';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

export default {
  preprocess: vitePreprocess(),
  kit: {
    adapter: adapter({
      pages: '../dist/ui',
      assets: '../dist/ui',
      fallback: '404.html'
    }),
    paths: {
      relative: true,
      //base: '/ui',   // enable ONLY if the URL path actually includes /ui
      //assets: '/ui'
    }
  }
};
