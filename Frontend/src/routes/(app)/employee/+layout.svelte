<script lang="ts">
    import { onMount, type Snippet } from 'svelte';
    import Navbar from '$lib/components/layout/Navbar.svelte';
	import { authGuard } from '../../common-guards.ts/user.guard';
	import { page } from '$app/stores';

    interface Props { children: Snippet; }
    let { children }: Props = $props();

    const tabs = [
        { label: 'Work schedule', href: '/employee/schedule' },
        { label: 'Absence',       href: '/employee/absence'  },
    ];
    $effect(() => {
		authGuard($page.url);
	});
</script>

<Navbar {tabs}/>

<main class="max-w-7xl mx-auto px-4 sm:px-6 py-6">
    {@render children()}
</main>