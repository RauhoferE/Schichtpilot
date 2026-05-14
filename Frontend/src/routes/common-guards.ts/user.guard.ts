import { goto } from "$app/navigation";
import { hasJwtToken, isJwtExpired } from "$lib/services/jwt.service";
import { redirect } from "@sveltejs/kit";

export function authGuard(url: URL) {
    // If no token, redirect to login
    console.log("hit")
    if (!hasJwtToken()) {
        goto(`/login?returnTo=${url.pathname}`);
        return;
    }

    try {
        
        if (isJwtExpired()) {
            goto('/login');
        }

        return; // Return user data if valid
    } catch {
        goto('/login');
    }
}