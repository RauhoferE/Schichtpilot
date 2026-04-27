//Without hooks.server.ts, a logged-out user could manually type /dashboard in the URL bar and SvelteKit would happily try to render it. 
// The hook prevents that entirely at the server level — before any data is loaded or any component is rendered.

import { redirect } from '@sveltejs/kit';
import type { Handle } from '@sveltejs/kit';

// Routes that require authentication
const PROTECTED_ROUTES = ['/dashboard', '/schedule', '/settings', '/profile'];
// Routes that should redirect authenticated users away (e.g., login page)
const AUTH_ROUTES = ['/login', '/register'];

export const handle: Handle = async ({ event, resolve }) => {
    const session = event.cookies.get('sp_session');

    const isProtected = PROTECTED_ROUTES.some((r) =>
        event.url.pathname.startsWith(r)
    );
    const isAuthRoute = AUTH_ROUTES.some((r) =>
        event.url.pathname.startsWith(r)
    );

    if (isProtected && !session) {
        redirect(303, `/login?redirectTo=${event.url.pathname}`);
    }

    if (isAuthRoute && session) {
        redirect(303, '/dashboard');
    }

    return resolve(event);
};