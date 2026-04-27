import { fail, redirect } from '@sveltejs/kit';
import type { Actions } from './$types';

export const actions: Actions = {
    default: async ({ request, fetch }) => {
        const data = await request.formData();

        const email           = data.get('email')?.toString().trim();
        const password        = data.get('password')?.toString();
        const confirmPassword = data.get('confirmPassword')?.toString();
        const firstName       = data.get('firstName')?.toString().trim();
        const lastName        = data.get('lastName')?.toString().trim();
        const birthdate       = data.get('birthdate')?.toString();
        const street          = data.get('street')?.toString().trim();
        const city            = data.get('city')?.toString().trim();
        const postalCode      = data.get('postalCode')?.toString();

        if (!email || !password || !confirmPassword || !firstName ||
            !lastName || !birthdate || !street || !city || !postalCode) {
            return fail(400, { error: 'All fields are required.' });
        }

        if (password !== confirmPassword) {
            return fail(400, { error: 'Passwords do not match.', email, firstName, lastName });
        }

        // Build payload matching backend RegisterDto — addressDto is nested
        const payload = {
            email,
            password,
            firstName,
            lastName,
            birthdate,
            addressDto: {
                street,
                city,
                postalCode: Number(postalCode),
            },
        };

        const response = await fetch('/api/auth/register', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify(payload),
        });

        if (!response.ok) {
            const body = await response.json().catch(() => ({}));
            return fail(response.status, {
                error: body.message ?? 'Registration failed. Please try again.',
                email, firstName, lastName,
            });
        }

        redirect(303, '/login');
    },
};