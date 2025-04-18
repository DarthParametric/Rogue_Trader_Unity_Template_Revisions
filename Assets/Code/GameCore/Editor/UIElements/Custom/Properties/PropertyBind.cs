using System;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements
{
	class PropertyBind<T> : IBinding
	{
		public PropertyBind(Func<T> valueGetter, Action<T> valueSetter, BaseField<T> field)
		{
			m_ValueSetter = valueSetter;
			m_ValueGetter = valueGetter;
			m_Field = field;

			field.RegisterValueChangedCallback(ValueChange);
		}

		private readonly BaseField<T> m_Field;

		private readonly Func<T> m_ValueGetter;

		private readonly Action<T> m_ValueSetter;

		void IBinding.PreUpdate() { }

		void IBinding.Release() { }

		void IBinding.Update()
		{
			m_Field.value = m_ValueGetter();
		}

		private void ValueChange(ChangeEvent<T> evnt)
		{
			m_ValueSetter(evnt.newValue);
		}
	}
}