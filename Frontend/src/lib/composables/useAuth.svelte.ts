import { goto } from '$app/navigation';
import { browser } from '$app/environment';

const MAX_ATTEMPTS = 5;
const LOCKOUT_DURATION = 30;
const MOCK_MODE = true;
const MOCK_USER_KEY = 'sp_mock_user';

interface AddressDto {
    street: string;
    city: string;
    postalCode: number | string;
}

interface RegisterPayload {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    birthdate: string;
    addressDto: AddressDto;
}

type MockUser = {
    email: string;
    password: string;
    role: 'Admin' | 'User';
};

function saveMockUser(user: MockUser) {
    if (!browser) return;
    localStorage.setItem(MOCK_USER_KEY, JSON.stringify(user));
}

function loadMockUser(): MockUser | null {
    if (!browser) return null;
    const raw = localStorage.getItem(MOCK_USER_KEY);
    if (!raw) return null;
    try {
        return JSON.parse(raw) as MockUser;
    } catch {
        return null;
    }
}

export interface PasswordStrength {
    score: number;
    label: 'Too short' | 'Weak' | 'Fair' | 'Good' | 'Strong';
    color: string;
    rules: {
        minLength: boolean;
        hasUpper: boolean;
        hasLower: boolean;
        hasDigit: boolean;
        hasSpecial: boolean;
        meetsPolicy: boolean;
    };
}

export function checkPasswordStrength(password: string): PasswordStrength {
    const minLength = password.length >= 12;
    const hasUpper = /[A-Z]/.test(password);
    const hasLower = /[a-z]/.test(password);
    const hasDigit = /[0-9]/.test(password);
    const hasSpecial = /[^A-Za-z0-9]/.test(password);

    const typesMet = [hasUpper, hasLower, hasDigit, hasSpecial].filter(Boolean).length;
    const meetsPolicy = minLength && typesMet >= 3;
    const score = !minLength ? 0 : Math.min(4, typesMet);

    const labels = ['Too short', 'Weak', 'Fair', 'Good', 'Strong'] as const;
    const colors = ['bg-muted', 'bg-red-500', 'bg-orange-400', 'bg-yellow-400', 'bg-green-500'];

    return {
        score,
        label: labels[score],
        color: colors[score],
        rules: { minLength, hasUpper, hasLower, hasDigit, hasSpecial, meetsPolicy }
    };
}

export function createLoginState() {
    let email = $state('');
    let password = $state('');
    let isLoading = $state(false);
    let errorMessage = $state('');
    let failedAttempts = $state(0);
    let isLockedOut = $state(false);
    let lockoutSeconds = $state(0);
    let lockoutTimer: ReturnType<typeof setInterval> | null = null;

    let isFormDisabled = $derived(isLoading || isLockedOut);

    function triggerLockout() {
        isLockedOut = true;
        lockoutSeconds = LOCKOUT_DURATION;
        errorMessage = '';
        lockoutTimer = setInterval(() => {
            lockoutSeconds -= 1;
            if (lockoutSeconds <= 0) {
                isLockedOut = false;
                failedAttempts = 0;
                if (lockoutTimer) clearInterval(lockoutTimer);
            }
        }, 1000);
    }

    async function handleLogin(event: SubmitEvent) {
        event.preventDefault();
        if (isFormDisabled) return;

        isLoading = true;
        errorMessage = '';

        try {
            if (MOCK_MODE) {
                const mockUser = loadMockUser();

                if (mockUser && email === mockUser.email && password === mockUser.password) {
                    failedAttempts = 0;
                    document.cookie = `sp_session=mock-session; path=/`;
                    document.cookie = `sp_role=${mockUser.role}; path=/`;
                    goto(mockUser.role === 'Admin' ? '/manager/dashboard' : '/employee/absence');
                    return;
                }

                failedAttempts += 1;
                if (failedAttempts >= MAX_ATTEMPTS) {
                    triggerLockout();
                } else {
                    const remaining = MAX_ATTEMPTS - failedAttempts;
                    errorMessage = `Invalid email or password. ${remaining} attempt${remaining === 1 ? '' : 's'} remaining.`;
                }
                return;
            }

            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password }),
                credentials: 'include'
            });

            const data = await response.json();

            if (response.ok) {
                if (data.token) sessionStorage.setItem('sp_token', data.token);
                failedAttempts = 0;
                goto(data.role === 'Admin' ? '/manager/dashboard' : '/employee/schedule');
            } else {
                failedAttempts += 1;
                if (failedAttempts >= MAX_ATTEMPTS) {
                    triggerLockout();
                } else {
                    const remaining = MAX_ATTEMPTS - failedAttempts;
                    errorMessage =
                        data.message ??
                        `Invalid email or password. ${remaining} attempt${remaining === 1 ? '' : 's'} remaining.`;
                }
            }
        } catch {
            errorMessage = 'Connection error. Please check your network and try again.';
        } finally {
            isLoading = false;
        }
    }

    return {
        get email() {
            return email;
        },
        set email(v: string) {
            email = v;
        },
        get password() {
            return password;
        },
        set password(v: string) {
            password = v;
        },
        get isLoading() {
            return isLoading;
        },
        get errorMessage() {
            return errorMessage;
        },
        get isLockedOut() {
            return isLockedOut;
        },
        get lockoutSeconds() {
            return lockoutSeconds;
        },
        get isFormDisabled() {
            return isFormDisabled;
        },
        handleLogin
    };
}

export function createRegisterState() {
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
    let successMessage = $state('');

    let isFormDisabled = $derived(isLoading);
    let passwordStrength = $derived(checkPasswordStrength(password));

    function validate(): string | null {
        if (
            !email ||
            !password ||
            !confirmPassword ||
            !firstName ||
            !lastName ||
            !birthdate ||
            !street ||
            !city ||
            !postalCode
        ) {
            return 'All fields are required.';
        }
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            return 'Please enter a valid email address.';
        }
        if (!passwordStrength.rules.minLength) {
            return 'Password must be at least 12 characters.';
        }
        if (!passwordStrength.rules.meetsPolicy) {
            return 'Password must contain at least 3 of: uppercase, lowercase, number, special character.';
        }
        if (password !== confirmPassword) {
            return 'Passwords do not match.';
        }
        if (firstName.length > 20) return 'First name must be 20 characters or fewer.';
        if (lastName.length > 20) return 'Last name must be 20 characters or fewer.';
        if (street.length > 50) return 'Street address must be 50 characters or fewer.';
        if (city.length > 20) return 'City must be 20 characters or fewer.';
        if (isNaN(Number(postalCode)) || Number(postalCode) <= 0) {
            return 'Please enter a valid postal code.';
        }
        return null;
    }

    async function handleRegister(event: SubmitEvent) {
        event.preventDefault();
        if (isFormDisabled) return;

        errorMessage = '';
        successMessage = '';

        const validationError = validate();
        if (validationError) {
            errorMessage = validationError;
            return;
        }

        isLoading = true;

        const payload: RegisterPayload = {
            email,
            password,
            firstName,
            lastName,
            birthdate,
            addressDto: {
                street,
                city,
                postalCode: Number(postalCode)
            }
        };

        try {
            if (MOCK_MODE) {
                const role: 'Admin' | 'User' = 'User';
                saveMockUser({ email, password, role });
                successMessage = 'Account created! Redirecting to login…';
                setTimeout(() => goto('/login'), 2000);
                return;
            }

            const response = await fetch('/api/auth/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            const data = await response.json();

            if (response.ok) {
                successMessage = data.message ?? 'Account created! Redirecting to login…';
                setTimeout(() => goto('/login'), 2000);
            } else {
                errorMessage = extractBackendError(data);
            }
        } catch {
            errorMessage = 'Connection error. Please check your network and try again.';
        } finally {
            isLoading = false;
        }
    }

    return {
        get email() {
            return email;
        },
        set email(v: string) {
            email = v;
        },
        get password() {
            return password;
        },
        set password(v: string) {
            password = v;
        },
        get confirmPassword() {
            return confirmPassword;
        },
        set confirmPassword(v: string) {
            confirmPassword = v;
        },
        get firstName() {
            return firstName;
        },
        set firstName(v: string) {
            firstName = v;
        },
        get lastName() {
            return lastName;
        },
        set lastName(v: string) {
            lastName = v;
        },
        get birthdate() {
            return birthdate;
        },
        set birthdate(v: string) {
            birthdate = v;
        },
        get street() {
            return street;
        },
        set street(v: string) {
            street = v;
        },
        get city() {
            return city;
        },
        set city(v: string) {
            city = v;
        },
        get postalCode() {
            return postalCode;
        },
        set postalCode(v: string) {
            postalCode = v;
        },
        get isLoading() {
            return isLoading;
        },
        get errorMessage() {
            return errorMessage;
        },
        get successMessage() {
            return successMessage;
        },
        get isFormDisabled() {
            return isFormDisabled;
        },
        get passwordStrength() {
            return passwordStrength;
        },
        handleRegister
    };
}

function extractBackendError(data: Record<string, unknown>): string {
    if (typeof data.message === 'string') return data.message;
    if (Array.isArray(data.errors)) {
        const first = data.errors[0] as { description?: string };
        return first?.description ?? 'Registration failed.';
    }
    if (data.errors && typeof data.errors === 'object') {
        const first = Object.values(data.errors as Record<string, string[]>)[0];
        if (Array.isArray(first) && first.length > 0) return first[0];
    }
    return 'Registration failed. Please try again.';
}