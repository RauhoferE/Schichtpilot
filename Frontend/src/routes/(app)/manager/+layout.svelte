<script lang="ts">
    import type { Snippet } from 'svelte';
    import Navbar from '$lib/components/layout/Navbar.svelte';

    interface Props { children: Snippet; }
    let { children }: Props = $props();

    // Manager navigation tabs 
    // Order matches the mockup: Overview first, then the rest
    const tabs = [
        { label: 'Overview',           href: '/manager/overview'    },
        { label: 'Teams',              href: '/manager/teams'       },
        { label: 'New Employee',       href: '/manager/employee'},
        { label: 'Time Management',    href: '/manager/time'        },
        { label: 'Absence Management', href: '/manager/absence'     },
        { label: 'Company Policy',     href: '/manager/policy'      },
    ];
</script>

<Navbar {tabs} />

<main class="max-w-7xl mx-auto px-4 sm:px-6 py-6">
    {@render children()}
</main>