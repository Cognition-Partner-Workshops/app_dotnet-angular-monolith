import React, { useState, useCallback } from 'react';

interface FormData {
    name: string;
    email: string;
    subject: string;
    message: string;
}

interface FormErrors {
    name?: string;
    email?: string;
    subject?: string;
    message?: string;
}

interface FormProps {
    actionUrl?: string;
    [key: string]: string | undefined;
}

export function FormComponent({ actionUrl }: FormProps): JSX.Element {
    const [formData, setFormData] = useState<FormData>({ name: '', email: '', subject: '', message: '' });
    const [errors, setErrors] = useState<FormErrors>({});
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitStatus, setSubmitStatus] = useState<'idle' | 'success' | 'error'>('idle');

    const validate = useCallback((data: FormData): FormErrors => {
        const errs: FormErrors = {};
        if (!data.name.trim()) errs.name = 'Name is required';
        if (!data.email.trim()) {
            errs.email = 'Email is required';
        } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(data.email)) {
            errs.email = 'Invalid email format';
        }
        if (!data.subject.trim()) errs.subject = 'Subject is required';
        if (!data.message.trim()) errs.message = 'Message is required';
        return errs;
    }, []);

    const handleChange = useCallback((e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
        setErrors(prev => ({ ...prev, [name]: undefined }));
    }, []);

    const handleSubmit = useCallback(async (e: React.FormEvent) => {
        e.preventDefault();
        const validationErrors = validate(formData);
        if (Object.keys(validationErrors).length > 0) {
            setErrors(validationErrors);
            return;
        }

        setIsSubmitting(true);
        try {
            const url = actionUrl || '/bin/devinreactaem/form';
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData),
            });
            if (!response.ok) throw new Error('Submission failed');
            setSubmitStatus('success');
            setFormData({ name: '', email: '', subject: '', message: '' });
        } catch {
            setSubmitStatus('error');
        } finally {
            setIsSubmitting(false);
        }
    }, [formData, validate, actionUrl]);

    return (
        <form className="react-form" onSubmit={handleSubmit} noValidate>
            <div className={`react-form-field ${errors.name ? 'react-form-field--error' : ''}`}>
                <label htmlFor="form-name">Name *</label>
                <input id="form-name" name="name" value={formData.name} onChange={handleChange} required />
                {errors.name && <span className="react-form-field__error-message">{errors.name}</span>}
            </div>

            <div className={`react-form-field ${errors.email ? 'react-form-field--error' : ''}`}>
                <label htmlFor="form-email">Email *</label>
                <input id="form-email" name="email" type="email" value={formData.email} onChange={handleChange} required />
                {errors.email && <span className="react-form-field__error-message">{errors.email}</span>}
            </div>

            <div className={`react-form-field ${errors.subject ? 'react-form-field--error' : ''}`}>
                <label htmlFor="form-subject">Subject *</label>
                <select id="form-subject" name="subject" value={formData.subject} onChange={handleChange} required>
                    <option value="">Select a subject</option>
                    <option value="general">General Inquiry</option>
                    <option value="support">Technical Support</option>
                    <option value="feedback">Feedback</option>
                    <option value="other">Other</option>
                </select>
                {errors.subject && <span className="react-form-field__error-message">{errors.subject}</span>}
            </div>

            <div className={`react-form-field ${errors.message ? 'react-form-field--error' : ''}`}>
                <label htmlFor="form-message">Message *</label>
                <textarea id="form-message" name="message" rows={5} value={formData.message} onChange={handleChange} required />
                {errors.message && <span className="react-form-field__error-message">{errors.message}</span>}
            </div>

            <button type="submit" className="react-form__submit" disabled={isSubmitting}>
                {isSubmitting ? 'Submitting...' : 'Submit'}
            </button>

            {submitStatus === 'success' && (
                <div className="react-form__success-message" role="alert">Thank you! Your message has been sent.</div>
            )}
            {submitStatus === 'error' && (
                <div className="react-form__error-message" role="alert">Something went wrong. Please try again.</div>
            )}
        </form>
    );
}
