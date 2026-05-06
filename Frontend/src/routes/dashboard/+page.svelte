<!-- src/routes/manager/dashboard/+page.svelte -->
<script>
    import { goto } from '$app/navigation';

    function logout() {
        document.cookie = 'sp_session=; path=/; max-age=0';
        document.cookie = 'sp_role=; path=/; max-age=0';
        goto('/login');
    }
</script>

<h1>Main Dashboard</h1>
<button onclick={logout}>Logout</button>