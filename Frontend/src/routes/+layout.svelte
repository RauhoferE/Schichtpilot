<script lang="ts">
    import './layout.css';
    import '../lib/styles/app.css';
    import favicon from '$lib/assets/favicon.svg';
    import type { Snippet } from 'svelte';
export const ssr = false;
export const prerender = false;
    interface Props {
        children: Snippet;
    }

    let { children }: Props = $props();
</script>

<svelte:head><link rel="/favicon.svg" href={favicon} /></svelte:head>
{@render children()}