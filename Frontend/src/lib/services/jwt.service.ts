import type { FrontendUser } from "$lib/types/user.types";
import Cookies from "js-cookie";
import { jwtDecode } from "jwt-decode";

function decodeFrontendToken(token: string): FrontendUser  | null{
    try {
        return jwtDecode<FrontendUser>(token);
        
    } catch {
        return null
    }
    
}

export function hasJwtToken(): boolean{
    const token = Cookies.get('SchichtpilotUser');

    if (token) {
        return true;
    }

    return false;
}

export function isJwtExpired(): boolean{
    const token = Cookies.get('SchichtpilotUser');

    if (!token) {
        return true;
    }

    return decodeFrontendToken(token)!.exp * 1000 < Date.now()
}

export function getUserEmail(): string {
    const token = Cookies.get('SchichtpilotUser');

    if (token) {
        return decodeFrontendToken(token)?.sub as string;
    }

    throw Error;
}

export function getUserName(): string {
    const token = Cookies.get('SchichtpilotUser');

    if (token) {
        const user = decodeFrontendToken(token);
        return `${user?.name} ${user?.family_name}`;
    }

    throw Error;
}

export function getUserRole(): string {
    const token = Cookies.get('SchichtpilotUser');

    if (token) {
        const user = decodeFrontendToken(token);
        
        if (user?.role == '') {
            throw Error;
        }
        return user?.role as string;
    }

    throw Error;
}

export function isAdmin(): boolean{
    const role = getUserRole();

    if (role == 'Admin') {
        return true;
    }

    return false;
}

export function removeCookie(): void{
    Cookies.remove('SchichtpilotUser');
}