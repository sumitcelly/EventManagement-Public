import DOMPurify from 'dompurify';
import * as Yup from 'yup';

/**
 * Custom Yup transformation to automatically sanitize rich text HTML strings.
 * It removes harmful scripts while keeping safe HTML layout elements.
 */
export const richTextSchemaValidator =  (richTextFieldName: string)=> Yup.string()
  .transform((value: string) => {
    if (!value) return '';
    // DOMPurify strips out <script>, onload events, and javascript: protocols
    return DOMPurify.sanitize(value);
  })
  .required(`${richTextFieldName} is required`);

export const richTextSchemaValidatorWithoutrequired =  Yup.string()
  .transform((value: string) => {
    if (!value) return '';
    // DOMPurify strips out <script>, onload events, and javascript: protocols
    return DOMPurify.sanitize(value);
  });



export const urlValidationSchema = Yup.string()
    // 1. Checks basic URL structure format
    .url('Please enter a valid URL structure (e.g., https://example.com)')
    // 2. Security Check: Block dangerous protocols like javascript:
    .test('safe-protocol', 'URL must start with http:// or https://', (value) => {
      if (!value) return true; // Let .required() handle empty fields
      return value.startsWith('http://') || value.startsWith('https://');
    });



