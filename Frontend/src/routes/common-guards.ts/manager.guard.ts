import { isAdmin } from "$lib/services/jwt.service";
import { redirect } from "@sveltejs/kit";
import { authGuard } from "./user.guard";

export function adminGuard(url: URL) {
    authGuard(url); // Guards can build on each other

    if (!isAdmin()) {
        throw redirect(307, '/unauthorized');
    }
}