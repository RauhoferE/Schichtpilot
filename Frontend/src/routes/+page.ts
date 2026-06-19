import { redirect } from '@sveltejs/kit';

export function load() {
    // Instantly intercepts the request to "/" and redirects to "/login"
    throw redirect(307, '/login');
}