import { isAdmin } from "$lib/services/jwt.service";
import { redirect } from "@sveltejs/kit";
import { authGuard } from "./user.guard";
import { goto } from "$app/navigation";

export function adminGuard(url: URL) {
    authGuard(url); // Guards can build on each other

    if (!isAdmin()) {
        goto('/login');
    }
}