<script lang="ts">
    import { onMount } from 'svelte';
    import { usePolicy } from '$lib/composables/usePolicy.svelte';

    import { Button } from '$lib/components/ui/button';
    import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '$lib/components/ui/card';
    import { Input } from '$lib/components/ui/input';
    import { Label } from '$lib/components/ui/label';
    import * as Alert from '$lib/components/ui/alert';
    import { DEFAULT_COMPANY_POLICY } from '$lib/types/policy.types';

    const policyState = usePolicy();

    let editingPolicy = $state(false);
    let editingHolidayIndex = $state<number | null>(null);

    onMount(async () => {
        await policyState.loadPolicy();
    });

    function toNumber(value: string, fallback = 0) {
        const parsed = Number(value);
        return Number.isNaN(parsed) ? fallback : parsed;
    }

    async function resetPolicyToDefault() {
        const confirmed = window.confirm('Reset policy to default values? This will overwrite the current values.');
        if (!confirmed) return;
        policyState.companyPolicy.minimumRestPeriodInMinutes = DEFAULT_COMPANY_POLICY.minimumRestPeriodInMinutes;
        policyState.companyPolicy.restPeriodThresholdInMinutes = DEFAULT_COMPANY_POLICY.restPeriodThresholdInMinutes;
        policyState.companyPolicy.maximumConsecutiveWorkHoursPerDay = DEFAULT_COMPANY_POLICY.maximumConsecutiveWorkHoursPerDay;
        policyState.companyPolicy.maximumConsecutiveWorkHoursPerWeek = DEFAULT_COMPANY_POLICY.maximumConsecutiveWorkHoursPerWeek;
        editingPolicy = false;
        await policyState.savePolicy();
    }
</script>

<svelte:head>
    <title>Company Policy — Schichtpilot</title>
</svelte:head>

<div class="policy-page">
    <div class="page-header">
        <div>
            <h1 class="text-xl font-semibold">Company Policy</h1><br />
            <p>Define the core scheduling limits and holiday settings for the company.</p>
        </div>
    </div>

    {#if policyState.loading}
        <Card>
            <CardHeader>
                <CardTitle>Loading</CardTitle>
                <CardDescription>Company policy is being loaded.</CardDescription>
            </CardHeader>
        </Card>
    {:else}
        {#if policyState.errorMessage}
            <Alert.Root variant="destructive">
                <Alert.Title>Error</Alert.Title>
                <Alert.Description>{policyState.errorMessage}</Alert.Description>
            </Alert.Root>
        {/if}

        {#if policyState.successMessage}
            <Alert.Root class="border-2 border-green-200 bg-green-50 text-green-900">
                <Alert.Title class="font-semibold text-green-800">Policy saved</Alert.Title>
                <Alert.Description class="text-green-700">{policyState.successMessage}</Alert.Description>
            </Alert.Root>
        {/if}

        <!-- ÜBERSICHTSLISTE -->
        <Card class="mb-6 border-2 border-amber-400">
            <CardHeader>
                <CardTitle class="font-semibold">Current Policy</CardTitle>
                <CardDescription>Overview of the currently saved company policy.</CardDescription>
            </CardHeader>
            <CardContent>

                <!-- Policy Werte -->
                <div class="mb-4">
                    <div class="flex items-center justify-between mb-2">
                        <span class="text-sm font-semibold">Work Hour Limits</span>
                        <div class="flex gap-2">
                            {#if !editingPolicy}
                                <Button size="sm" variant="outline" onclick={() => editingPolicy = true}>Edit</Button>
                            {:else}
                                <Button size="sm" variant="outline" onclick={() => editingPolicy = false}>Cancel</Button>
                            {/if}
                            <Button size="sm" variant="destructive" onclick={resetPolicyToDefault}>
                                Reset to Default
                            </Button>
                        </div>
                    </div>

                    {#if !editingPolicy}
                        <div class="grid grid-cols-2 gap-4 text-sm">
                            <div class="flex flex-col gap-1">
                                <span class="text-muted-foreground">Minimum rest period</span>
                                <span class="font-medium">{policyState.companyPolicy.minimumRestPeriodInMinutes} min</span>
                            </div>
                            <div class="flex flex-col gap-1">
                                <span class="text-muted-foreground">Rest threshold</span>
                                <span class="font-medium">{policyState.companyPolicy.restPeriodThresholdInMinutes} min</span>
                            </div>
                            <div class="flex flex-col gap-1">
                                <span class="text-muted-foreground">Max. work hours / day</span>
                                <span class="font-medium">{policyState.companyPolicy.maximumConsecutiveWorkHoursPerDay} h</span>
                            </div>
                            <div class="flex flex-col gap-1">
                                <span class="text-muted-foreground">Max. work hours / week</span>
                                <span class="font-medium">{policyState.companyPolicy.maximumConsecutiveWorkHoursPerWeek} h</span>
                            </div>
                        </div>
                    {:else}
                        <div class="grid grid-cols-2 gap-4 text-sm">
                            <div class="flex flex-col gap-1">
                                <Label>Minimum rest period (min)</Label>
                                <Input type="number" min="5" max="120"
                                       value={policyState.companyPolicy.minimumRestPeriodInMinutes}
                                       oninput={(e) => (policyState.companyPolicy.minimumRestPeriodInMinutes = toNumber(e.currentTarget.value))} />
                            </div>
                            <div class="flex flex-col gap-1">
                                <Label>Rest threshold (min)</Label>
                                <Input type="number" min="30" max="480"
                                       value={policyState.companyPolicy.restPeriodThresholdInMinutes}
                                       oninput={(e) => (policyState.companyPolicy.restPeriodThresholdInMinutes = toNumber(e.currentTarget.value))} />
                            </div>
                            <div class="flex flex-col gap-1">
                                <Label>Max. work hours / day</Label>
                                <Input type="number" min="1" max="12"
                                       value={policyState.companyPolicy.maximumConsecutiveWorkHoursPerDay}
                                       oninput={(e) => (policyState.companyPolicy.maximumConsecutiveWorkHoursPerDay = toNumber(e.currentTarget.value))} />
                            </div>
                            <div class="flex flex-col gap-1">
                                <Label>Max. work hours / week</Label>
                                <Input type="number" min="1" max="60"
                                       value={policyState.companyPolicy.maximumConsecutiveWorkHoursPerWeek}
                                       oninput={(e) => (policyState.companyPolicy.maximumConsecutiveWorkHoursPerWeek = toNumber(e.currentTarget.value))} />
                            </div>
                        </div>
                    {/if}
                </div>

                <!-- Holidays Liste -->
                <div>
                    <span class="text-sm font-semibold">Holidays</span>
                    {#if policyState.holidays.holidays.length === 0}
                        <p class="text-sm text-muted-foreground mt-2">No holidays defined yet.</p>
                    {:else}
                        <div class="flex flex-col gap-2 mt-2">
                            {#each policyState.holidays.holidays as holiday, index}
                                <div class="flex items-center justify-between rounded-md border border-amber-300 px-3 py-2 text-sm">
                                    {#if editingHolidayIndex === index}
                                        <Input
                                                type="date"
                                                class="h-7 w-40"
                                                value={holiday}
                                                oninput={(e) => policyState.updateHoliday(index, e.currentTarget.value)}
                                        />
                                        <Button size="sm" variant="outline" onclick={() => editingHolidayIndex = null}>
                                            Done
                                        </Button>
                                    {:else}
                                        <span class="font-medium">{holiday}</span>
                                        <div class="flex gap-2">
                                            <Button size="sm" variant="outline" onclick={() => editingHolidayIndex = index}>
                                                Edit
                                            </Button>
                                            <Button size="sm" variant="destructive" onclick={() => policyState.removeHolidayAt(index)}>
                                                Delete
                                            </Button>
                                        </div>
                                    {/if}
                                </div>
                            {/each}
                        </div>
                    {/if}
                </div>
            </CardContent>
        </Card>

        <!-- EINGABEMASKE -->
        <Card class="mb-4 border border-muted">
            <CardHeader>
                <CardTitle>Add new Company Policy</CardTitle>
                <CardDescription>Enter new values for the company policy and holidays.</CardDescription>
            </CardHeader>
            <CardContent>
                <div class="policy-grid">
                    <Card>
                        <CardHeader>
                            <CardTitle>Rest Period Parameters</CardTitle>
                            <CardDescription>Configure the minimum required rest rules.</CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div class="form-grid two-columns">
                                <div class="field">
                                    <Label for="minimumRestPeriodInMinutes">Minimum rest period (minutes)</Label>
                                    <Input
                                            id="minimumRestPeriodInMinutes"
                                            type="number" min="5" max="120"
                                            value={policyState.companyPolicy.minimumRestPeriodInMinutes}
                                            oninput={(e) => (policyState.companyPolicy.minimumRestPeriodInMinutes = toNumber(e.currentTarget.value))}
                                    />
                                </div>
                                <div class="field">
                                    <Label for="restPeriodThresholdInMinutes">Rest threshold (minutes)</Label>
                                    <Input
                                            id="restPeriodThresholdInMinutes"
                                            type="number" min="30" max="480"
                                            value={policyState.companyPolicy.restPeriodThresholdInMinutes}
                                            oninput={(e) => (policyState.companyPolicy.restPeriodThresholdInMinutes = toNumber(e.currentTarget.value))}
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle>Maximum Work Hours</CardTitle>
                            <CardDescription>Configure work-hour limits per day and week.</CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div class="form-grid two-columns">
                                <div class="field">
                                    <Label for="maximumConsecutiveWorkHoursPerDay">
                                        Maximum consecutive work hours per day
                                    </Label>
                                    <Input
                                            id="maximumConsecutiveWorkHoursPerDay"
                                            type="number" min="1" max="12"
                                            value={policyState.companyPolicy.maximumConsecutiveWorkHoursPerDay}
                                            oninput={(e) => (policyState.companyPolicy.maximumConsecutiveWorkHoursPerDay = toNumber(e.currentTarget.value))}
                                    />
                                </div>
                                <div class="field">
                                    <Label for="maximumConsecutiveWorkHoursPerWeek">
                                        Maximum consecutive work hours per week
                                    </Label>
                                    <Input
                                            id="maximumConsecutiveWorkHoursPerWeek"
                                            type="number" min="1" max="60"
                                            value={policyState.companyPolicy.maximumConsecutiveWorkHoursPerWeek}
                                            oninput={(e) => (policyState.companyPolicy.maximumConsecutiveWorkHoursPerWeek = toNumber(e.currentTarget.value))}
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle>Holiday Parameters</CardTitle>
                            <CardDescription>Define holiday dates used during scheduling.</CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div class="flex flex-col gap-4">
                                {#if policyState.holidays.holidays.length === 0}
                                    <p class="text-sm text-muted-foreground">No holiday dates added yet.</p>
                                {/if}

                                {#each policyState.holidays.holidays as holiday, index}
                                    <div class="flex items-end gap-4">
                                        <div class="flex flex-1 flex-col gap-2">
                                            <Label for={`holiday-${index}`}>Holiday date</Label>
                                            <Input
                                                    id={`holiday-${index}`}
                                                    type="date"
                                                    value={holiday}
                                                    oninput={(e) => policyState.updateHoliday(index, e.currentTarget.value)}
                                            />
                                        </div>
                                        <div class="flex items-end">
                                            <Button type="button" variant="outline" onclick={() => policyState.removeHolidayAt(index)}>
                                                Remove
                                            </Button>
                                        </div>
                                    </div>
                                {/each}

                                <Button type="button" onclick={() => policyState.addHoliday()}>
                                    Add Holiday
                                </Button>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            </CardContent>
        </Card>

        <!-- BUTTONS AM SCHLUSS -->
        <div class="flex justify-end gap-3 mt-4">
            <Button variant="outline" onclick={policyState.resetAll} disabled={policyState.saving}>
                Reset
            </Button>
            <Button onclick={async () => { await policyState.savePolicy(); editingPolicy = false; editingHolidayIndex = null; }} disabled={policyState.saving}>
                {policyState.saving ? 'Saving...' : 'Save Policy'}
            </Button>
        </div>
    {/if}
</div>