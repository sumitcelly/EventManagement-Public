// EventForm.tsx
import React from "react";
import { useForm, Controller } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import RichTextEditor from "../../components/RichTextEditor";


const eventSchema = yup.object({
  title: yup.string().required("Title is required"),
  eventDate: yup.string().required("Event date is required"),
  description: yup
    .string()
    .test("not-empty", "Description is required", (value) => {
      const stripped = value?.replace(/<[^>]+>/g, "").trim(); // remove HTML tags
      return !!stripped;
    })
    .required("Description is required"),
  terms: yup
    .string()
    .test("not-empty", "Terms are required", (value) => {
      const stripped = value?.replace(/<[^>]+>/g, "").trim();
      return !!stripped;
    })
    .required("Terms are required"),
});

type FormValues = {
  title: string;
  description: string;
  terms: string;
  eventDate: string;
};

export default function EventForm() {
  const {
    control,
    handleSubmit,
    register,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: yupResolver(eventSchema),
    defaultValues: {
      title: "",
      description: "",
      terms: "",
      eventDate: "",
    },
  });

  const onSubmit = (data: FormValues) => {
    console.log("✅ Submitted data:", data);
  };

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="max-w-2xl mx-auto p-6 space-y-6"
    >
      <div>
        <label className="block font-semibold mb-1">Event Title</label>
        <input
          {...register("title")}
          className="w-full border rounded p-2"
          placeholder="Enter event title"
        />
        {errors.title && (
          <p className="text-red-600 text-sm mt-1">{errors.title.message}</p>
        )}
      </div>

      <div>
        <label className="block font-semibold mb-1">Event Date</label>
        <input
          {...register("eventDate")}
          type="date"
          className="w-full border rounded p-2"
        />
        {errors.eventDate && (
          <p className="text-red-600 text-sm mt-1">{errors.eventDate.message}</p>
        )}
      </div>

      {/* Rich Text Field 1 */}
      <div>
        <label className="block font-semibold mb-1">Description</label>
        <Controller
          name="description"
          control={control}
          render={({ field }) => (
            <RichTextEditor value={field.value} onChange={field.onChange} />
          )}
        />
        {errors.description && (
          <p className="text-red-600 text-sm mt-1">
            {errors.description.message}
          </p>
        )}
      </div>

      {/* Rich Text Field 2 */}
      <div>
        <label className="block font-semibold mb-1">Terms & Conditions</label>
        <Controller
          name="terms"
          control={control}
          render={({ field }) => (
            <RichTextEditor value={field.value} onChange={field.onChange} />
          )}
        />
        {errors.terms && (
          <p className="text-red-600 text-sm mt-1">{errors.terms.message}</p>
        )}
      </div>

      <button
        type="submit"
        className="ml-auto bg-brand-dark text-white text-brand-neutral px-4 py-2 rounded hover:bg-blue-700"
      >
        Submit
      </button>
    </form>
  );
}
