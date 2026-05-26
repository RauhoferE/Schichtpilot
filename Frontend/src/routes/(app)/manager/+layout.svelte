<script lang="ts">
    import { onMount, type Snippet } from 'svelte';
    import Navbar from '$lib/components/layout/Navbar.svelte';
	import { authGuard } from '../../common-guards.ts/user.guard';
	import { page } from '$app/stores';
	import { adminGuard } from '../../common-guards.ts/manager.guard';
	import type { LayoutLoad } from '../../$types';

    interface Props { children: Snippet; }
    let { children }: Props = $props();

    // Manager navigation tabs 
    // Order matches the mockup: Overview first, then the rest
    const tabs = [
        { label: 'Overview',           href: '/manager/overview'    },
        { label: 'Teams',              href: '/manager/teams'       },
        { label: 'New Employee',       href: '/manager/employee'},
        { label: 'Shift Management',     href: '/manager/shifts'      },
        { label: 'Job Role Management',     href: '/manager/jobrole'      },
        { label: 'Time Management',    href: '/manager/time'        },
        { label: 'Absence Management', href: '/manager/absence'     },
        { label: 'Company Policy',     href: '/manager/policy'      },
    ];

    $effect(() => {
		adminGuard($page.url);
	});
</script>

<Navbar {tabs} />

<main class="max-w-7xl mx-auto px-4 sm:px-6 py-6">
    {@render children()}
</main>