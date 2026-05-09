// Without hooks.server.ts, a logged-out user could manually type a protected URL
// in the URL bar and SvelteKit would happily try to render it.
// The hook prevents that entirely at the server level — before any data is loaded
// or any component is rendered.

import { redirect } from '@sveltejs/kit';
import type { Handle } from '@sveltejs/kit';

// Route groups 

// Routes that require the user to be logged in
const PROTECTED_ROUTES = [
    '/employee',
    '/manager',
];

// Routes that logged-in users should be redirected away from
const AUTH_ROUTES = ['/login', '/register'];

// Routes only accessible by Admin role
const ADMIN_ROUTES = ['/manager'];

//  Default landing pages per role 
const EMPLOYEE_HOME = '/employee/schedule';
const ADMIN_HOME    = '/manager/overview';

// Hook 
export const handle: Handle = async ({ event, resolve }) => {
    const session = event.cookies.get('sp_session');
    const role    = event.cookies.get('sp_role');

    // Populate locals.user so +page.server.ts files can access it
    // MOCK ONLY: in production this would validate a real JWT/session token
    event.locals.user = session
        ? {
            id:      session,
            email:   event.cookies.get('sp_email') ?? undefined,
            role:    role as 'User' | 'Admin',
            session,
        }
        : null;

    const isProtected = PROTECTED_ROUTES.some(r => event.url.pathname.startsWith(r));
    const isAuthRoute = AUTH_ROUTES.some(r => event.url.pathname.startsWith(r));
    const isAdminRoute = ADMIN_ROUTES.some(r => event.url.pathname.startsWith(r));

    // 1. Not logged in + trying to access a protected route → send to login
    if (isProtected && !session) {
        throw redirect(303, `/login?redirectTo=${event.url.pathname}`);
    }

    // 2. Logged in as non-admin + trying to access admin route → send to employee home
    if (isAdminRoute && session && role !== 'Admin') {
        throw redirect(303, EMPLOYEE_HOME);
    }

    // 3. Already logged in + trying to access login/register → send to their home
    if (isAuthRoute && session) {
        throw redirect(303, role === 'Admin' ? ADMIN_HOME : EMPLOYEE_HOME);
    }

    // Sync user data to a client-readable cookie for store hydration
    // MOCK ONLY: remove httpOnly: false in production
    if (event.locals.user) {
        event.cookies.set('user', JSON.stringify(event.locals.user), {
            path:     '/',
            httpOnly: false, // allows client JS to read it — set true in production
            secure:   false, // set true in production (HTTPS only)
        });
    }

    return resolve(event);
};