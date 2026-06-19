import { browser } from '$app/environment';
import { writable } from 'svelte/store';

export interface User {
    id: string;
    email?: string;
    role: 'User' | 'Admin';
    token?: string;
    session?: string;
}

const { subscribe, set } = writable<User | null>(null);

const authStore = {
    subscribe,

    login(user: User) {
        set(user);
        if (browser) {
            localStorage.setItem('user', JSON.stringify(user));
        }
    },

    logout() {
        set(null);
        if (browser) {
            localStorage.removeItem('user');
        }
    },

    init() {
        if (browser) {
            const stored = localStorage.getItem('user');
            if (stored) {
                try {
                    set(JSON.parse(stored));
                } catch {
                    this.logout();
                }
            }
        }
    }
};

export default authStore;