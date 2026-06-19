<script lang="ts">
    import * as Card from '$lib/components/ui/card/index.js';
    import * as Alert from '$lib/components/ui/alert/index.js';
    import { Button } from '$lib/components/ui/button/index.js';
    import { Input } from '$lib/components/ui/input/index.js';
    import { Label } from '$lib/components/ui/label/index.js';
    import { CalendarClock, Regex } from 'lucide-svelte';
    import * as EmailValidator from 'email-validator';
	import { HttpError, ValidationError } from '$lib/customErrors';
	import { createUser } from '$lib/services/user.service';
	import { redirect } from '@sveltejs/kit';
	import { goto } from '$app/navigation';

    let email = $state('');
    let password = $state('');
    let confirmPassword = $state('');
    let firstName = $state('');
    let lastName = $state('');
    let birthdate = $state('');
    let street = $state('');
    let city = $state('');
    let postalCode = $state('');
    let isLoading = $state(false);
    let errorMessage = $state('');
    let submitted = $state(false);

    let touched = $state({email:'', password:'', firstName:'', lastName: '', birthdate:'', street:'', city:'',postalCode:''});

    function handleBlur(field: string) {
        touched[field] = true;
    }

    let emailError = $derived.by(()=>{
            if (email == '' || !EmailValidator.validate(email)) {
                return 'Email address is required';
            }
            
            return '';
    });

    let passwordError = $derived.by(()=>{
        if (password == '') {
            return 'A password is required';
        }

        if (!/^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*#?&])[A-Za-z\\d@$!%*#?&]{12,}$/.test(password)) {
            return 'Password needs to be atleast 12 characters long and contain 1 uppercase, 1 lowercase, 1 number and 1 special character';
        }

        if (confirmPassword == '') {
            return 'Please confirm the password';
        }

        if (confirmPassword != password) {
            return 'Passwords must match';
        }

        return '';
    });

    let birthdateError = $derived.by(()=>{
        if (birthdate == '') {
            return 'Birthdate is required';
        }

        if (new Date(birthdate) > new Date()) {
            return 'Birthdate cant be in the future';
        }

        return '';
    });

    let firstNameError = $derived.by(()=>{
        if (firstName == '') {
            return 'Name is required';
        }

        if (firstName.length < 3 || firstName.length > 20) {
            return 'Name must be between 3 and 20 characters long';
        }

        return '';
    });

    let lastNameError = $derived.by(()=>{
        if (firstName == '') {
            return 'Last Name is required';
        }

        if (lastName.length < 3 || lastName.length > 20) {
            return 'Last Name must be between 3 and 20 characters long';
        }

        return '';
    });

    let postalCodeError = $derived.by(()=>{
        if (postalCode == '' || isNaN(Number(postalCode))) {
            return 'Postal Code is required';
        }

        let pce = Number(postalCode);

        if (pce < 1000 || pce > 9999) {
            return 'Postal Code is invalid';
        }

        return '';
    });

    let cityError = $derived.by(()=>{
        if (city == '') {
            return 'City is required';
        }

        if (city.length < 2 || city.length > 20) {
            return 'City must be between 2 and 20 characters long';
        }

        return '';
    });

    let streetError = $derived.by(()=>{
        if (street == '') {
            return 'Street is required';
        }

        if (street.length < 2 || street.length > 20) {
            return 'Street must be between 2 and 20 characters long';
        }

        return '';
    });

    function isFormValid(): boolean{

        let isValid = true;

        if (emailError != '') {
            isValid = false;
        }

        if (firstNameError != '') {
            isValid = false;
        }

        if (lastNameError != '') {
            isValid = false;
        }

        if (birthdateError != '') {
            isValid = false;
        }

        if (passwordError != '') {
            isValid = false;
        }

        if (streetError != '') {
            isValid = false;
        }

        if (postalCodeError != '') {
            isValid = false;
        }

        if (cityError != '') {
            isValid = false;
        }

        return isValid;
    }

    async function handleRegister(event: SubmitEvent) {
        event.preventDefault();

        errorMessage = '';
        submitted = true;
        
        if (!isFormValid()) {
            return;
        }

        isLoading = true;
        try {
            await createUser({
                email,
                password,
                firstName,
                lastName,
                birthDate: new Date(birthdate),
                addressDto: {
                    street,
                    city,
                    postalCode: Number(postalCode)
                }
            });

            
            isLoading = false;
            //TODO: Replace with information website
            await goto('/login');
        } catch (error) {
            console.log(error);
            errorMessage = 'Registration failed.';
            if (error instanceof(ValidationError)) {
                let modelErrorText = '';
                error.ValidationError.errorStates.forEach(element => {
                    modelErrorText = modelErrorText + `${element.fieldName}: ${element.message}\n`
                });

                errorMessage = modelErrorText;
            }

            if (error instanceof(HttpError)) {
                errorMessage = error.Error.message;
            }
            
            isLoading = false;
        } 
    }
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
            {#if errorMessage}
                <Alert.Root variant="destructive" class="mb-4">
                    <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                        <circle cx="12" cy="12" r="10"/>
                        <line x1="12" y1="8" x2="12" y2="12"/>
                        <line x1="12" y1="16" x2="12.01" y2="16"/>
                    </svg>
                    <Alert.Title>Registration failed</Alert.Title>
                    <Alert.Description>{errorMessage}</Alert.Description>
                </Alert.Root>
            {/if}

            <form class="space-y-6" onsubmit={handleRegister} novalidate>

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
                                    onblur={()=>handleBlur('firstName')}
                                    disabled={isLoading}
                                    bind:value={firstName}
                            />
                            {#if firstNameError != '' && (touched['firstName'] || submitted)}
                            <span class="error">
                             {firstNameError}
                            </span>
                            {/if}

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
                                    onblur={()=>handleBlur('lastName')}
                                    disabled={isLoading}
                                    bind:value={lastName}
                            />
                            {#if lastNameError != '' && (touched['lastName'] || submitted)}
                            <span class="error">
                             {lastNameError}
                            </span>
                            {/if}
                        </div>
                    </div>

                    <div class="space-y-2">
                        <Label for="birthdate">Date of Birth</Label>
                        <Input
                                id="birthdate"
                                type="date"
                                autocomplete="bday"
                                required
                                onblur={()=>handleBlur('birthdate')}
                                disabled={isLoading}
                                bind:value={birthdate}
                        />
                            {#if birthdateError != '' && (touched['birthdate'] || submitted)}
                            <span class="error">
                             {birthdateError}
                            </span>
                            {/if}
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
                                onblur={()=>handleBlur('street')}
                                disabled={isLoading}
                                bind:value={street}
                        />
                            {#if streetError != '' && (touched['street'] || submitted)}
                            <span class="error">
                             {streetError}
                            </span>
                            {/if}
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
                                    onblur={()=>handleBlur('city')}
                                    disabled={isLoading}
                                    bind:value={city}
                            />
                            {#if cityError != '' && (touched['city'] || submitted)}
                            <span class="error">
                             {cityError}
                            </span>
                            {/if}
                        </div>
                        <div class="space-y-2">
                            <Label for="postalCode">Postal Code</Label>
                            <Input
                                    id="postalCode"
                                    type="number"
                                    placeholder="1060"
                                    autocomplete="postal-code"
                                    required
                                    onblur={()=>handleBlur('postalCode')}
                                    disabled={isLoading}
                                    bind:value={postalCode}
                            />
                            {#if postalCodeError != '' && (touched['postalCode'] || submitted)}
                            <span class="error">
                             {postalCodeError}
                            </span>
                            {/if}
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
                                onblur={()=>handleBlur('email')}
                                disabled={isLoading}
                                bind:value={email}
                        />
                            {#if emailError != '' && (touched['email'] || submitted)}
                            <span class="error">
                             {emailError}
                            </span>
                            {/if}
                    </div>

                    <div class="space-y-2">
                        <Label for="password">Password</Label>
                        <Input
                                id="password"
                                type="password"
                                placeholder="••••••••••"
                                autocomplete="new-password"
                                required
                                onblur={()=>handleBlur('password')}
                                disabled={isLoading}
                                bind:value={password}
                        />
                        {#if passwordError != '' && (touched['password'] || submitted)}
                            <span class="error">
                             {passwordError}
                            </span>
                            {/if}
                    </div>

                    <div class="space-y-2">
                        <Label for="confirmPassword">Confirm Password</Label>
                        <Input
                                id="confirmPassword"
                                type="password"
                                placeholder="••••••••••"
                                autocomplete="new-password"
                                required
                                disabled={isLoading}
                                bind:value={confirmPassword}
                        />
                    </div>
                </fieldset>

                <Button type="submit" class="w-full" disabled={isLoading} aria-busy={isLoading}>
                    {#if isLoading}
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
