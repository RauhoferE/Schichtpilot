<!--
  routes/register/+page.svelte
  Uses shadcn-svelte v1 components: Card, Input, Label, Button
  All fields match backend User + AddressDto exactly.
-->
<script lang="ts">
    import * as Card from '$lib/components/ui/card/index.js';
    import { Button }  from '$lib/components/ui/button/index.js';
    import { Input }   from '$lib/components/ui/input/index.js';
    import { Label }   from '$lib/components/ui/label/index.js';
    import { CalendarClock  } from 'lucide-svelte';
    import { createRegisterState } from '$lib/composables/useAuth.svelte';

    const auth = createRegisterState();
</script>

<svelte:head>
    <title>Register — Schichtpilot</title>
</svelte:head>

<main class="min-h-dvh grid place-items-center bg-muted/40 px-4 py-10">
    <Card.Root class="w-full max-w-lg">

        <Card.Header class="text-center space-y-1">
            <!-- Schichtpilot calendar-clock/pilot icon -->
            <div class="flex items-center justify-center gap-2 mb-2">
                <CalendarClock  class="w-8 h-8 text-black" />
                <Card.Title class="text-2xl font-bold" style="color: #F59E0B;" >Schichtpilot</Card.Title>
            </div>
            <Card.Description>Create your account</Card.Description>
        </Card.Header>

        <Card.Content>

            <!-- Error alert -->
            {#if auth.errorMessage}
                <div class="flex items-start gap-3 rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-800 mb-4" role="alert" aria-live="polite">
                    <svg class="w-4 h-4 mt-0.5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                        <circle cx="12" cy="12" r="10"/>
                        <line x1="12" y1="8" x2="12" y2="12"/>
                        <line x1="12" y1="16" x2="12.01" y2="16"/>
                    </svg>
                    <p>{auth.errorMessage}</p>
                </div>
            {/if}

            <!-- Success alert -->
            {#if auth.successMessage}
                <div class="flex items-start gap-3 rounded-md border border-green-200 bg-green-50 p-3 text-sm text-green-800 mb-4" role="alert" aria-live="polite">
                    <svg class="w-4 h-4 mt-0.5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                        <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                        <polyline points="22 4 12 14.01 9 11.01"/>
                    </svg>
                    <p>{auth.successMessage}</p>
                </div>
            {/if}

            <form class="space-y-6" onsubmit={auth.handleRegister} novalidate>

                <!-- ── Personal Information ── -->
                <fieldset class="space-y-4">
                    <legend class="text-xs font-semibold uppercase tracking-widest text-muted-foreground">
                        Personal Information
                    </legend>

                    <div class="grid grid-cols-2 gap-4">
                        <div class="space-y-2">
                            <Label for="firstName">First Name</Label>
                            <Input
                                    id="firstName"
                                    type="text"
                                    placeholder="Hannah"
                                    autocomplete="given-name"
                                    maxlength={20}
                                    required
                                    disabled={auth.isFormDisabled}
                                    bind:value={auth.firstName}
                            />
                        </div>
                        <div class="space-y-2">
                            <Label for="lastName">Last Name</Label>
                            <Input
                                    id="lastName"
                                    type="text"
                                    placeholder="Müller"
                                    autocomplete="family-name"
                                    maxlength={20}
                                    required
                                    disabled={auth.isFormDisabled}
                                    bind:value={auth.lastName}
                            />
                        </div>
                    </div>

                    <div class="space-y-2">
                        <Label for="birthdate">Date of Birth</Label>
                        <Input
                                id="birthdate"
                                type="date"
                                autocomplete="bday"
                                required
                                disabled={auth.isFormDisabled}
                                bind:value={auth.birthdate}
                        />
                    </div>
                </fieldset>

                <div class="border-t"></div>

                <!-- ── Address → AddressDto ── -->
                <fieldset class="space-y-4">
                    <legend class="text-xs font-semibold uppercase tracking-widest text-muted-foreground">
                        Address
                    </legend>

                    <div class="space-y-2">
                        <Label for="street">Street Address</Label>
                        <Input
                                id="street"
                                type="text"
                                placeholder="Mariahilfer Straße 12"
                                autocomplete="street-address"
                                maxlength={50}
                                required
                                disabled={auth.isFormDisabled}
                                bind:value={auth.street}
                        />
                    </div>

                    <div class="grid grid-cols-2 gap-4">
                        <div class="space-y-2">
                            <Label for="city">City</Label>
                            <Input
                                    id="city"
                                    type="text"
                                    placeholder="Vienna"
                                    autocomplete="address-level2"
                                    maxlength={20}
                                    required
                                    disabled={auth.isFormDisabled}
                                    bind:value={auth.city}
                            />
                        </div>
                        <div class="space-y-2">
                            <Label for="postalCode">Postal Code</Label>
                            <Input
                                    id="postalCode"
                                    type="number"
                                    placeholder="1060"
                                    autocomplete="postal-code"
                                    required
                                    disabled={auth.isFormDisabled}
                                    bind:value={auth.postalCode}
                            />
                        </div>
                    </div>
                </fieldset>

                <div class="border-t"></div>

                <!-- ── Account Credentials ── -->
                <fieldset class="space-y-4">
                    <legend class="text-xs font-semibold uppercase tracking-widest text-muted-foreground">
                        Account
                    </legend>

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
                                autocomplete="new-password"
                                required
                                disabled={auth.isFormDisabled}
                                bind:value={auth.password}
                        />
                    </div>

                    <div class="space-y-2">
                        <Label for="confirmPassword">Confirm Password</Label>
                        <Input
                                id="confirmPassword"
                                type="password"
                                placeholder="••••••••••"
                                autocomplete="new-password"
                                required
                                disabled={auth.isFormDisabled}
                                bind:value={auth.confirmPassword}
                        />
                    </div>
                </fieldset>

                <Button type="submit" class="w-full" disabled={auth.isFormDisabled} aria-busy={auth.isLoading}>
                    {#if auth.isLoading}
                        <span class="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" aria-hidden="true"></span>
                        Creating account…
                    {:else}
                        Register
                    {/if}
                </Button>

            </form>
        </Card.Content>

        <Card.Footer class="justify-center text-sm text-muted-foreground">
            Already registered?
            <a href="/login" class="ml-1 font-semibold text-primary underline underline-offset-4 hover:opacity-80">Login</a>
        </Card.Footer>

    </Card.Root>
</main>