using AssetRipper.Assets.Generics;
using AssetRipper.SourceGenerated.Extensions.Enums.AnimationClip.GenericBinding;
using AssetRipper.SourceGenerated.Subclasses.AnimationClipBindingConstant;
using AssetRipper.SourceGenerated.Subclasses.GenericBinding;

namespace AssetRipper.SourceGenerated.Extensions
{
	public class FastClipBindingLookup : IComparable<FastClipBindingLookup>
	{
		public int idx;
		public IGenericBinding binding;

		public int CompareTo(FastClipBindingLookup other)
		{
			return idx.CompareTo(other.idx);
		}

		public int CompareTo(int other)
		{
			return idx.CompareTo(other);
		}

		public FastClipBindingLookup(int i, IGenericBinding b)
		{
			idx = i;
			binding = b;
		}
	}
	public static class AnimationClipBindingConstantExtensions
	{
		public static int UpperBound(List<FastClipBindingLookup> values, int target, int first, int last)
		{
			int left = first;
			int right = last;

			while (left < right)
			{
				int mid = left + (right - left) / 2;
				var middle = values[mid];

				if (middle.CompareTo(target) > 0)
					right = mid;
				else
					left = mid + 1;
			}

			return left;
		}
		public static List<FastClipBindingLookup> PrepareFastLookup(this IAnimationClipBindingConstant constant)
		{
			List<FastClipBindingLookup> res = new();
			int idx = 0;
			var bindings = constant.GenericBindings;

			for (int i = 0; i < bindings.Count; i++)
			{
				IGenericBinding gb = bindings[i];
				int increment = 1;
				if (gb.GetClassID() == ClassIDType.Transform)
				{
					increment = gb.TransformType().GetDimension();
				}

				idx += increment;
				res.Add(new FastClipBindingLookup(idx, gb));
			}

			return res;
		}
		public static IGenericBinding FindBinding(this IAnimationClipBindingConstant constant, List<FastClipBindingLookup> lookup, int index)
		{
			int curves = 0;
			int loc_idx = UpperBound(lookup, index, 0, lookup.Count);
			if (loc_idx < 0)
			{
				loc_idx = ~loc_idx;
			}
			else if (lookup[loc_idx].idx == index)
			{
				loc_idx -= 1;
			}

			if (loc_idx >= lookup.Count)
			{
				throw new ArgumentException($"Binding with index {index} hasn't been found", nameof(index));
			}

			return lookup[loc_idx].binding;
		}
	}
}
