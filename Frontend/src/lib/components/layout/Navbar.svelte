<script lang="ts">
    import { page } from '$app/state';
    import { goto } from '$app/navigation';
    import favicon from '$lib/assets/favicon.svg';

    // ── Types ─────────────────────────────────────────────────────────────────
    interface Tab {
        label: string;
        href: string;
    }

    interface Props {
        tabs: Tab[];
    }

    let { tabs }: Props = $props();

    // ── Logout ────────────────────────────────────────────────────────────────
    // MOCK ONLY: clears session from sessionStorage and redirects to login.
    // When backend is connected, replace with a real API call:
    // await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' });
    function handleLogout() {
            // Clear sessionStorage
            sessionStorage.removeItem('sp_session');
            sessionStorage.removeItem('sp_role');
            // Clear cookies so hooks.server.ts stops redirecting away from /login
            document.cookie = 'sp_session=; path=/; max-age=0';
            document.cookie = 'sp_role=; path=/; max-age=0';
            document.cookie = 'user=; path=/; max-age=0';
            window.location.href = '/login';
    }
</script>

<!--
    ── Navbar ────────────────────────────────────────────────────────────────────
    Shared navigation bar used by both employee and manager layouts.
    Tabs are passed in as props so each layout controls its own navigation items.
    Active tab is highlighted using the current URL pathname.

    Employee tabs:  Work schedule | Absence
    Manager tabs:   Dashboard | Work schedule | Absence | Employees

    Design decisions:
    - Logo text is amber to make the brand warm and distinct from UI elements
    - Tabs are styled as pill buttons for clarity with older/non-technical users
    ─────────────────────────────────────────────────────────────────────────────
-->
<header class="bg-white border-b border-border shadow-sm">
    <div class="max-w-7xl mx-auto px-4 sm:px-6">

        <!-- ── Top row: Logo + right actions ── -->
        <div class="flex items-center justify-between h-16">

            <!-- Logo: amber text to distinguish brand from blue UI elements -->
            <button
                    onclick={() => goto(tabs[0].href)}
                    class="flex items-center gap-2.5 font-bold text-base hover:opacity-80 transition-opacity"
            >
                <img src={favicon} alt="SchichtPilot" class="w-10 h-10" />
                <span style="color: #F59E0B; font-size: 22px;">SchichtPilot</span>
            </button>

            <!-- Right side: profile icon + logout button -->
            <div class="flex items-center gap-4">

                <!-- Profile icon: no action yet, will link to profile page when built -->
                <button
                        class="p-2 rounded-full hover:bg-muted transition-colors text-muted-foreground hover:text-amber-400"
                        aria-label="Profile"
                >
                    <svg class="w8 h-8" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="12" cy="8" r="4"/>
                        <path d="M4 20c0-4 3.6-7 8-7s8 3 8 7"/>
                    </svg>
                </button>

                <!-- Logout button: clears session and redirects to /login -->
                <button
                        onclick={handleLogout}
                        class="inline-flex items-center gap-1.5 px-4 py-2 rounded-md text-sm font-medium
                           bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
                >
                    <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
                        <polyline points="16 17 21 12 16 7"/>
                        <line x1="21" y1="12" x2="9" y2="12"/>
                    </svg>
                    Logout
                </button>
            </div>
        </div>

        <!-- ── Tab row ────────────────────────────────────────────────────────
             Pill-style buttons instead of anchor tabs to avoid Svelte parser
             issues with dynamic href inside each blocks.

             Active tab:   filled blue pill, white text
             Inactive tab: light grey pill, dark text, blue on hover
        ─────────────────────────────────────────────────────────────────────-->
        <div class="flex gap-2 pb-3">
            {#each tabs as tab}
                <button
                        onclick={() => goto(tab.href)}
                        class="px-5 py-2 rounded-full text-sm font-semibold transition-colors border
                        {page.url.pathname === tab.href
                            ? 'bg-amber-400 text-amber-900 border-amber-400'
                            : 'bg-muted text-foreground border-border hover:bg-amber-50 hover:text-amber-800 hover:border-amber-400'}"
                >
                    {tab.label}
                </button>
            {/each}
        </div>

    </div>
</header>