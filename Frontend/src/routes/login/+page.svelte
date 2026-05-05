<script lang="ts">
    import * as Card  from '$lib/components/ui/card/index.js';
    import * as Alert from '$lib/components/ui/alert/index.js';
    import { Button } from '$lib/components/ui/button/index.js';
    import { Input }  from '$lib/components/ui/input/index.js';
    import { Label }  from '$lib/components/ui/label/index.js';
    import { CalendarClock  } from 'lucide-svelte';
    import { createLoginState } from '$lib/composables/useAuth.svelte';

    const auth = createLoginState();
</script>

<svelte:head>
    <title>Login — Schichtpilot</title>
</svelte:head>

<main class="min-h-dvh grid place-items-center bg-muted/40 px-4 py-10">
    <Card.Root class="w-full max-w-md">
        <!-- Schichtpilot calendar-clock/pilot icon -->
        <Card.Header class="text-center space-y-1">
            <div class="flex items-center justify-center gap-2 mb-2">
                <CalendarClock  class="w-8 h-8 text-primary" />  
                <Card.Title class="text-2xl font-bold">Schichtpilot</Card.Title>
            </div>
            <Card.Description>Sign in to your workspace</Card.Description>
        </Card.Header>

        <Card.Content class="space-y-4">

            <!-- Lockout alert -->
            {#if auth.isLockedOut}
                <Alert.Root variant="destructive">
                    <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                        <rect x="3" y="11" width="18" height="11" rx="2"/>
                        <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                    </svg>
                    <Alert.Title>Account temporarily locked</Alert.Title>
                    <Alert.Description>
                        Too many failed attempts. Try again in {auth.lockoutSeconds}s.
                    </Alert.Description>
                </Alert.Root>
            {/if}

            <!-- Error alert -->
            {#if auth.errorMessage && !auth.isLockedOut}
                <Alert.Root variant="destructive">
                    <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                        <circle cx="12" cy="12" r="10"/>
                        <line x1="12" y1="8" x2="12" y2="12"/>
                        <line x1="12" y1="16" x2="12.01" y2="16"/>
                    </svg>
                    <Alert.Title>Login failed</Alert.Title>
                    <Alert.Description>{auth.errorMessage}</Alert.Description>
                </Alert.Root>
            {/if}

            <!-- Form -->
            <form class="space-y-4" onsubmit={auth.handleLogin} novalidate>
                <div class="space-y-2">
                    <Label for="email">Email</Label>
                    <Input
                            id="email"
                            type="email"
                            placeholder="hannah@companymail.at"
                            autocomplete="email"
                            required
                            disabled={auth.isFormDisabled}
                            bind:value={auth.email}
                    />
                </div>

                <div class="space-y-2">
                    <Label for="password">Password</Label>
                    <Input
                            id="password"
                            type="password"
                            placeholder="••••••••••"
                            autocomplete="current-password"
                            required
                            disabled={auth.isFormDisabled}
                            bind:value={auth.password}
                    />
                </div>

                <Button type="submit" class="w-full" disabled={auth.isFormDisabled} aria-busy={auth.isLoading}>
                    {#if auth.isLoading}
                        <span class="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" aria-hidden="true"></span>
                        Signing in…
                    {:else if auth.isLockedOut}
                        Locked ({auth.lockoutSeconds}s)
                    {:else}
                        Login
                    {/if}
                </Button>
            </form>

        </Card.Content>

        <Card.Footer class="flex flex-col items-center gap-3 text-sm">
            <a href="/forgot-password" class="text-primary underline underline-offset-4 hover:opacity-80">
                Forgot password?
            </a>
            <p class="text-muted-foreground">
                Do not have an Account yet?
                <a href="/register" class="font-semibold text-primary underline underline-offset-4 hover:opacity-80">Register</a>
            </p>
        </Card.Footer>

    </Card.Root>
</main>