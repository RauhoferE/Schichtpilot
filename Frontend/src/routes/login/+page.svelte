<script lang="ts">
    import * as Card  from '$lib/components/ui/card/index.js';
    import * as Alert from '$lib/components/ui/alert/index.js';
    import { Button } from '$lib/components/ui/button/index.js';
    import { Input }  from '$lib/components/ui/input/index.js';
    import { Label }  from '$lib/components/ui/label/index.js';
    import { CalendarClock  } from 'lucide-svelte';
    import { authenticate } from '$lib/services/auth.service';
    import * as EmailValidator from 'email-validator';
	import { isAdmin } from '$lib/services/jwt.service';
	import { redirect } from '@sveltejs/kit';
	import { goto } from '$app/navigation';

    let email = $state('');
    let password = $state('');
    let isLoading = $state(false);
    let errorMessage = $state('');

    let emailFieldValid = $derived(email != '' && EmailValidator.validate(email));
    let passwordFieldValid = $derived(password != '');
    let formValid = $derived(emailFieldValid && passwordFieldValid);

    let emailFieldTouched = $state(false);
    let passwordFieldTouched = $state(false);

    async function handleLogin(event: SubmitEvent) {
        event.preventDefault();
        if (!formValid) {
            emailFieldTouched = true;
            passwordFieldTouched = true;
            return;
        }

        errorMessage = '';
        isLoading = true;
        try {
            await authenticate({ email, password });
            isLoading = false;
            goto(isAdmin() ? '/manager/overview' : '/employee/schedule');
        } catch (error) { 
            errorMessage = 'Login failed.';
        }
        isLoading = false;
    }
</script>

<svelte:head>
    <title>Login — Schichtpilot</title>
</svelte:head>

<main class="min-h-dvh grid place-items-center bg-muted/40 px-4 py-10">
    <Card.Root class="w-full max-w-md">

        <!-- Logo + title -->
        <Card.Header class="text-center space-y-1">
            <div class="flex items-center justify-center gap-2 mb-2">
                <!-- Icon: amber to match brand color -->
                <CalendarClock class="w-8 h-8 text-black"/>
                <!-- Title: amber to match brand color -->
                <Card.Title class="text-2xl font-bold" style="color: #F59E0B;">Schichtpilot</Card.Title>
            </div>
            <Card.Description>Sign in to your workspace</Card.Description>
        </Card.Header>

        <Card.Content class="space-y-4">

            <!-- Error alert -->
            {#if errorMessage}
                <Alert.Root variant="destructive">
                    <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                        <circle cx="12" cy="12" r="10"/>
                        <line x1="12" y1="8" x2="12" y2="12"/>
                        <line x1="12" y1="16" x2="12.01" y2="16"/>
                    </svg>
                    <Alert.Title>Login failed</Alert.Title>
                    <Alert.Description>{errorMessage}</Alert.Description>
                </Alert.Root>
            {/if}

            <!-- Form -->
            <form class="space-y-4" onsubmit={handleLogin} novalidate>
                <div class="space-y-2">
                    <Label for="email">Email</Label>
                    <Input
                            id="email"
                            type="email"
                            placeholder="hannah@companymail.at"
                            autocomplete="email"
                            required
                            onblur={()=>emailFieldTouched = true}
                            disabled={isLoading}
                            bind:value={email}
                    />
                    {#if !emailFieldValid && emailFieldTouched}
                    <span class="error">
                        Email is required
                    </span>
                    {/if}

                </div>

                <div class="space-y-2">
                    <Label for="password">Password</Label>
                    <Input
                            id="password"
                            type="password"
                            placeholder="••••••••••"
                            autocomplete="current-password"
                            required
                            onblur={()=>passwordFieldTouched = true}
                            disabled={isLoading}
                            bind:value={password}
                    />
                    {#if !passwordFieldValid && passwordFieldTouched}
                    <span class="error">
                        Password is required
                    </span>
                    {/if}
                </div>

                <Button type="submit" class="w-full" disabled={isLoading || !formValid} aria-busy={isLoading}>
                    {#if isLoading}
                        <span class="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" aria-hidden="true"></span>
                        Signing in…
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
