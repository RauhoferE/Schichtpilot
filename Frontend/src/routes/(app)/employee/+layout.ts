import type { LayoutLoad } from "./$types";
import {authGuard} from '../../common-guards.ts/user.guard';

export const load: LayoutLoad = ({url}) =>{
    authGuard(url);
    return{};
}