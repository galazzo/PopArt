#pragma once

using namespace Windows::Foundation;
using namespace Windows::Storage::Streams;

using namespace Nokia::Graphics::Imaging;
using namespace Nokia::InteropServices::WindowsRuntime;

namespace NativeFilters
{	
	public ref class Halftone  sealed : ICustomEffect
    {
    public:
		Halftone();
		
		property unsigned int CellSize
		{
			unsigned int get()
			{
				return m_cell_size;
			}

			void set(unsigned int value)
			{
				m_cell_size = value;
			}
		}

		virtual IAsyncAction^ LoadAsync();
		virtual void Process(Windows::Foundation::Rect rect);
		virtual IBuffer^ ProvideSourceBuffer(Windows::Foundation::Size imageSize);
		virtual IBuffer^ ProvideTargetBuffer(Windows::Foundation::Size imageSize);

	private:
		double** tone[10];
		unsigned int m_cell_size;
		unsigned int imageWidth;
		Buffer^ sourceBuffer;
		Buffer^ targetBuffer;

	private:
		static byte* GetPointerToPixelData(IBuffer^ pixelBuffer, unsigned int *length);
		static void CreateDot(double*** dot, unsigned int outersize, unsigned int innersize);
    };
}