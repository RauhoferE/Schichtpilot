import { hasJwtToken, isJwtExpired } from "$lib/services/jwt.service";
import { redirect } from "@sveltejs/kit";

export function authGuard(url: URL) {
    // If no token, redirect to login
    if (!hasJwtToken()) {
        throw redirect(307, `/login?returnTo=${url.pathname}`);
    }

    try {
        
        if (isJwtExpired()) {
            throw redirect(307, '/login');
        }

        return; // Return user data if valid
    } catch {
        throw redirect(307, '/login');
    }
}