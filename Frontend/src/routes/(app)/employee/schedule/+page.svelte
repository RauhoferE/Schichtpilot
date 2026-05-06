<script lang="ts">
    import { goto } from '$app/navigation';

    function logout() {
        document.cookie = 'sp_session=; path=/; max-age=0';
        document.cookie = 'sp_role=; path=/; max-age=0';
        goto('/login');
    }

    const shifts = [
        { day: 'Monday', time: '08:00 - 16:00', role: 'Kitchen' },
        { day: 'Wednesday', time: '12:00 - 20:00', role: 'Service' },
        { day: 'Friday', time: '09:00 - 17:00', role: 'Cashier' }
    ];
</script>

<div class="space-y-6 p-6">
    <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">WELCOME TO EMPLOYEES SCHEDULE</h1>
        <button class="rounded bg-red-600 px-4 py-2 text-white" onclick={logout}>Logout</button>
    </div>

    <div class="rounded-lg border p-4">
        <h2 class="mb-3 text-lg font-semibold">Upcoming shifts</h2>
        <ul class="space-y-2">
            {#each shifts as shift}
                <li class="flex justify-between rounded border px-3 py-2">
                    <span>{shift.day}</span>
                    <span>{shift.time}</span>
                    <span>{shift.role}</span>
                </li>
            {/each}
        </ul>
    </div>
</div>