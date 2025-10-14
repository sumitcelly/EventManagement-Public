import { useEditor, EditorContent,Editor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Underline from '@tiptap/extension-underline'
import Image from '@tiptap/extension-image'
import TextAlign from '@tiptap/extension-text-align'
import { useEffect } from 'react'

interface Props {
  value?: string
  onChange?: (content: string) => void
}

export default function RichTextEditor({ value = '', onChange }: Props) {

    const editorInstance=  useEditor({
    extensions: [
      StarterKit,
      //Underline,
      Image,
      TextAlign.configure({ types: ['heading', 'paragraph'] }),
    ],
    content: value,
    editorProps: {
      attributes: {
        class:
          'focus:outline-none min-h-[150px] p-2 text-gray-900  [&_ol]:list-decimal [&_ul]:list-disc',
      },
    },
    onUpdate: ({ editor }) => {
      onChange?.(editor.getHTML())
    },
  })
    const editor = editorInstance as Editor | null
  // Keep editor content in sync with parent value
  useEffect(() => {
    if (editor && value !== editor.getHTML()) {
      editor.commands.setContent(value)
    }
  }, [value, editor])

  if (!editor) return null

  const handleButton = (command: () => void) => (e: React.MouseEvent) => {
    e.preventDefault() // prevent button from stealing focus
    command()
  }

  return (
    <div className="border rounded-lg p-3 bg-white prose max-w-none">
      {/* Toolbar */}
      <div className="flex gap-2 border-b pb-2 mb-2 flex-wrap">
        {/* Bold */}
        <button
          type="button"
          onMouseDown={handleButton(() => editor.chain().focus().toggleBold().run())}
          className={`px-2 py-1 rounded ${editor.isActive('bold') ? 'bg-blue-100' : ''}`}
        >
          <b>B</b>
        </button>

        {/* Italic */}
        <button
          type="button"
          onMouseDown={handleButton(() => editor.chain().focus().toggleItalic().run())}
          className={`px-2 py-1 rounded ${editor.isActive('italic') ? 'bg-blue-100' : ''}`}
        >
          <i>I</i>
        </button>

        {/* Underline */}
        <button
          type="button"
          onMouseDown={handleButton(() => editor.chain().focus().toggleUnderline().run())}
          className={`px-2 py-1 rounded ${editor.isActive('underline') ? 'bg-blue-100' : ''}`}
        >
          U
        </button>

        {/* Bullet List */}
        <button
          type="button"
          onMouseDown={handleButton(() => editor.chain().focus().toggleBulletList().run())}
          className={`px-2 py-1 rounded ${editor.isActive('bulletList') ? 'bg-blue-100' : ''}`}
        >
          • List
        </button>

        {/* Ordered List */}
        <button
          type="button"
          onMouseDown={handleButton(() => editor.chain().focus().toggleOrderedList().run())}
          className={`px-2 py-1 rounded ${editor.isActive('orderedList') ? 'bg-blue-100' : ''}`}
        >
          1. List
        </button>

        {/* Text Alignment */}
        {['left', 'center', 'right', 'justify'].map((align) => (
          <button
            key={align}
            type="button"
            onMouseDown={handleButton(() => editor.chain().focus().setTextAlign(align as any).run())}
            className={`px-2 py-1 rounded ${
              editor.isActive({ textAlign: align as any }) ? 'bg-blue-100' : ''
            }`}
          >
            {align.charAt(0).toUpperCase() + align.slice(1)}
          </button>
        ))}

       
      </div>

      {/* Editor Content */}
      <EditorContent editor={editor} className="min-h-[150px]" />
    </div>
  )
}
