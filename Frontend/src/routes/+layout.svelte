<!--
  +layout.svelte — Root layout
  This is the only place app.css is imported.
  Every page in the app inherits this layout automatically.
-->
<script lang="ts">
    import './layout.css';
    import '../lib/styles/app.css';
    import favicon from '$lib/assets/favicon.svg';
    import type { Snippet } from 'svelte';

    interface Props {
        children: Snippet;
    }

    let { children }: Props = $props();
</script>

<svelte:head><link rel="icon" href={favicon} /></svelte:head>
{@render children()}