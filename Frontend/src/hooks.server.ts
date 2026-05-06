// Without hooks.server.ts, a logged-out user could manually type /dashboard 
// in the URL bar and SvelteKit would happily try to render it.
// The hook prevents that entirely at the server level — before any data is loaded or any component is rendered.

import { redirect } from '@sveltejs/kit';
import type { Handle } from '@sveltejs/kit';

// Routes that require authentication
const PROTECTED_ROUTES = [
    '/dashboard',
    '/schedule',
    '/settings',
    '/profile',
    '/users',
    '/absences',
    '/employee',
    '/manager',
];

// Routes that should redirect authenticated users away (e.g., login page)
const AUTH_ROUTES = ['/login', '/register'];

export const handle: Handle = async ({ event, resolve }) => {
    const session = event.cookies.get('sp_session');
    const role = event.cookies.get('sp_role');

    // ✅ Populate locals.user for +page.server.ts and client sync
    event.locals.user = session ? {
        id: session,
        email: event.cookies.get('sp_email') || undefined, // if available
        role: role as 'User' | 'Admin',
        session
    } : null;

    const isProtected = PROTECTED_ROUTES.some((r) => event.url.pathname.startsWith(r));
    const isAuthRoute = AUTH_ROUTES.some((r) => event.url.pathname.startsWith(r));
    const isAdminRoute = ['/users'].some((r) => event.url.pathname.startsWith(r));

    // 1. Not logged in → go to login
    if (isProtected && !session) {
        throw redirect(303, `/login?redirectTo=${event.url.pathname}`);
    }

    // 2. Logged in but wrong role → go to dashboard
    if (isAdminRoute && role !== 'Admin') {
        throw redirect(303, '/dashboard');
    }

    // 3. Already logged in → skip login/register
    if (isAuthRoute && session) {
        throw redirect(303, '/dashboard');
    }

    // ✅ Sync full user data to cookie for client store hydration
    if (event.locals.user) {
        event.cookies.set('user', JSON.stringify(event.locals.user), {
            path: '/',
            httpOnly: false,  // Allow client JS access
            secure: false     // Set true in production
        });
    }

    return resolve(event);
};