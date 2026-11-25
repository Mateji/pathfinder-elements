import { type AbstractControl, FormArray, FormControl, FormGroup, type ValidatorFn } from '@angular/forms';
import {
    type AttackDamage, type AttackDefinition, type AttackMode, type AttackSequence, type FieldIssue, type LibraryTemplate,
    type StatisticField, statisticFields
} from './library-model';

const minimumInteger = -2147483648;
const maximumInteger = 2147483647;

export function createTemplateForm(template?: LibraryTemplate) {
    const statistics = Object.fromEntries(statisticFields.map((key) => {
        const minimum = key === 'maxHitPoints' ? 1 : minimumInteger;
        return [key, wholeNumber(template?.statistics[key], minimum)];
    })) as Record<StatisticField, FormControl<number | null>>;

    return new FormGroup({
        name: shortText(template?.name),
        statistics: new FormGroup(statistics),
        attackSequences: new FormArray((template?.attackSequences ?? []).map(createSequenceForm))
    });
}

export function createSequenceForm(sequence?: AttackSequence) {
    return new FormGroup({
        name: shortText(sequence?.name),
        attacks: new FormArray(sequence ? sequence.attacks.map(createAttackForm) : [createAttackForm()], {
            validators: (control) => {
                return (control as FormArray).length === 0 ? { required: true } : null;
            }
        })
    });
}

export function createAttackForm(attack?: AttackDefinition) {
    return new FormGroup({
        name: shortText(attack?.name),
        mode: new FormControl<AttackMode>(attack?.mode ?? 'melee', { nonNullable: true }),
        isNatural: toggle(attack?.isNatural),
        targetsTouchArmorClass: toggle(attack?.targetsTouchArmorClass),
        attackBonus: wholeNumber(attack?.attackBonus),
        criticalThreatMinimum: wholeNumber(attack?.criticalThreatMinimum ?? 20, 2, 20),
        criticalMultiplier: wholeNumber(attack?.criticalMultiplier ?? 2, 2, 10),
        damage: new FormArray((attack?.damage ?? []).map(createDamageForm)),
        notes: shortText(attack?.notes ?? undefined, 1024, false)
    });
}

export function createDamageForm(damage?: AttackDamage) {
    return new FormGroup({
        damageType: shortText(damage?.damageType),
        diceCount: wholeNumber(damage?.diceCount, 0, 100),
        dieSides: wholeNumber(damage?.dieSides, 0, 100),
        modifier: wholeNumber(damage?.modifier ?? 0),
        multipliesOnCritical: toggle(damage?.multipliesOnCritical)
    }, {
        validators: (control) => {
            const diceCount: unknown = control.get('diceCount')?.value;
            const dieSides: unknown = control.get('dieSides')?.value;
            if (typeof diceCount !== 'number' || typeof dieSides !== 'number') {
                return null;
            }
            return (diceCount === 0 && dieSides !== 0) || (diceCount > 0 && dieSides < 2) ? { invalidDieSides: true } : null;
        }
    });
}

export function formIssues(control: AbstractControl, path = ''): FieldIssue[] {
    const issues = Object.keys(control.errors ?? {}).map((code) => ({ path, code }));
    if (control instanceof FormArray) {
        control.controls.forEach((child, index) => {
            issues.push(...formIssues(child, `${path}[${index}]`));
        });
    } else if (control instanceof FormGroup) {
        const children = Object.entries(control.controls) as [string, AbstractControl][];
        for (const [field, child] of children) {
            issues.push(...formIssues(child, path ? `${path}.${field}` : field));
        }
    }
    return issues;
}

export function moveFormItem<Control extends AbstractControl>(array: FormArray<Control>, index: number, offset: -1 | 1): void {
    const next = index + offset;
    if (index >= 0 && index < array.length && next >= 0 && next < array.length) {
        const item = array.at(index) as Control;
        array.removeAt(index);
        array.insert(next, item);
        array.markAsDirty();
    }
}

function shortText(value = '', maximumLength = 256, required = true): FormControl<string> {
    const validate: ValidatorFn = (control) => {
        const text = String(control.value);
        if (required && !text.trim()) {
            return { required: true };
        }
        if (text.length > maximumLength) {
            return { tooLong: true };
        }
        return text.includes('\0') ? { invalidCharacters: true } : null;
    };
    return new FormControl(value, { nonNullable: true, validators: validate });
}

function wholeNumber(value?: number, minimum = minimumInteger, maximum = maximumInteger): FormControl<number | null> {
    const validate: ValidatorFn = (control) => {
        const number: unknown = control.value;
        if (number === null) {
            return { required: true };
        }
        if (typeof number !== 'number' || !Number.isInteger(number) || number < minimum || number > maximum) {
            return { outOfRange: true };
        }
        return null;
    };
    return new FormControl(value ?? null, { validators: validate });
}

function toggle(value = false): FormControl<boolean> {
    return new FormControl(value, { nonNullable: true });
}
