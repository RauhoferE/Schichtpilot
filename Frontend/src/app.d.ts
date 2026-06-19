/// for information about these interfaces

import type { User } from '$lib/stores/auth';

declare global {
	namespace App {
		interface Locals {
			user: User | null;
		}
	}
}

export {};