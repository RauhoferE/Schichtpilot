<script lang="ts">
    import type { Snippet } from 'svelte';
    import Navbar from '$lib/components/layout/Navbar.svelte';

    interface Props { children: Snippet; }
    let { children }: Props = $props();

    const tabs = [
        { label: 'Dashboard',     href: '/manager/dashboard' },
        { label: 'Work schedule', href: '/manager/schedule'  },
        { label: 'Absence',       href: '/manager/absence'   },
        { label: 'Employees',     href: '/manager/employees' },
    ];
</script>

<Navbar {tabs} />

<main class="max-w-7xl mx-auto px-4 sm:px-6 py-6">
    {@render children()}
</main>