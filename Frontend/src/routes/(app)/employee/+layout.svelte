<script lang="ts">
    import type { Snippet } from 'svelte';

    interface Props {
        children: Snippet;
    }

    let { children }: Props = $props();
</script>

<main class="max-w-7xl mx-auto px-4 sm:px-6 py-6">
    {@render children()}
</main>