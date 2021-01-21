using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Matrix.Common.Agreements
{
	/// <summary>
	/// единицы измерения,
	/// название из СИ,
	/// * - x
	/// / - _
	/// TODO додумать...
	/// </summary>
	[DataContract]
	public enum MeasuringUnitType
	{
		/// <summary>
		/// неизвестная единица измерения
		/// </summary>
		[Parameter("unknown")]
		[EnumMember]
		Unknown,

		/// <summary>
		/// кило Паскаль (КПа)
		/// </summary>
		[Parameter("кПа")]
		[EnumMember]
		kPa,

		/// <summary>
		/// градус цельсия (°С)
		/// </summary>
		[Parameter("'С")]
		[EnumMember]
		C,

		/// <summary>
		/// метр в кубе (м³)
		/// </summary>
		[Parameter("м³")]
		[EnumMember]
		m3,

		/// <summary>
		/// час (ч)
		/// </summary>
		[Parameter("ч")]
		[EnumMember]
		h,

		/// <summary>
		/// Кельвин (К)
		/// </summary>
		[Parameter("К")]
		[EnumMember]
		K,

		/// <summary>
		/// тысяч метров в кубе (1000×м³)
		/// </summary>
		[Parameter("м³*1000")]
		[EnumMember]
		m3x1000,

		/// <summary>
		/// метр в кубе за час (м³/ч)
		/// </summary>
		[Parameter("м³/ч")]
		[EnumMember]
		m3_h,

		/// <summary>
		/// килограмм (кг)
		/// </summary>
		[Parameter("кг")]
		[EnumMember]
		kg,

		/// <summary>
		/// мега Джоуль на метр в кубе (МДж/м³)
		/// </summary>
		[Parameter("МДж/м³")]
		[EnumMember]
		MJ_m3,

		/// <summary>
		/// микроПаскаль в секунду (мкПа*с)
		/// </summary>
		[Parameter("мкПа*с")]
		[EnumMember]
		mkPa_s,
		/// <summary>
		/// мега Паскаль (МПа)
		/// </summary>
		[Parameter("МПа")]
		[EnumMember]
		MPa,

		[Parameter("бар")]
		[EnumMember]
		Bar,

		//IM2300
		[Parameter("Кг*с/см²")]
		[EnumMember]
		kgs_kvSm,

		[Parameter("Кг*с/м²")]
		[EnumMember]
		kgs_kvM,

		[Parameter("Мм.рт.ст.")]
		[EnumMember]
		mmRtSt,

		[Obsolete]
		[Parameter("м³/ч")]
		[EnumMember]
		kubM_h,

		[Parameter("т*м³/ч")]
		[EnumMember]
		tKubM_h,

		[Parameter("м")]
		[EnumMember]
		m,

		[Parameter("см")]
		[EnumMember]
		cm,

		[Parameter("ГКал")]
		[EnumMember]
		Gkal,

		[Parameter("ГКал/ч")]
		[EnumMember]
		Gkal_h,

		[Obsolete]
		[Parameter("м³")]
		[EnumMember]
		kubM,

		[Parameter("тм³")]
		[EnumMember]
		tKubM,

		[Parameter("т")]
		[EnumMember]
		tonn,

		[Parameter("кг/ч")]
		[EnumMember]
		kg_h,

		[Parameter("т/ч")]
		[EnumMember]
		tonn_h,

		[Parameter("л/Сек")]
		[EnumMember]
		l_sek,

		[Parameter("л")]
		[EnumMember]
		litr,

		[Parameter("нм³/ч")]
		[EnumMember]
		nKubM_h,

		[Parameter("тыс.нм³/ч")]
		[EnumMember]
		tNKubM_h,

		[Parameter("нм³")]
		[EnumMember]
		nKubM,

        [Parameter("тыс.нм³")]
		[EnumMember]
		tNKubM,

		[Parameter("ч:мин")]
		[EnumMember]
		hrMin,

		[Parameter("%")]
		[EnumMember]
		prs,

		[Parameter("кг/м³")]
		[EnumMember]
		kg_kubM,

		[Parameter("м³/сут")]
		[EnumMember]
		kubM_sut,

		[Parameter("Вт*ч")]
		[EnumMember]
		WtH,

		[Parameter("кВт*ч")]
		[EnumMember]
		kWtH,

		[Parameter("МВт*ч")]
		[EnumMember]
		MWtH,

		[Parameter("МДж")]
		[EnumMember]
		MDj,

		[Parameter("ГДж")]
		[EnumMember]
		GDj,

		[Parameter("Вт")]
		[EnumMember]
		Wt,
		[Parameter("кВт")]
		[EnumMember]
		kWt,
		[Parameter("МВт")]
		[EnumMember]
		MWt,

		[Parameter("А")]
		[EnumMember]
		A,
		[Parameter("кА")]
		[EnumMember]
		kA,
		[Parameter("мА")]
		[EnumMember]
		mA,

		[Parameter("Гц")]
		[EnumMember]
		Hz,
		[Parameter("кГц")]
		[EnumMember]
		kHz,

		[Parameter("сек")]
		[EnumMember]
		sec,
		[Parameter("мин")]
		[EnumMember]
		min,
		[Parameter("д")]
		[EnumMember]
		day,
		[Parameter("Дж")]
		[EnumMember]
		Dj,
		[Parameter("м³/мин")]
		[EnumMember]
		m3_min,
		[Parameter("м³/с")]
		[EnumMember]
		m3_s,

		[Parameter("В")]
		[EnumMember]
		V,
        
        [Parameter("ГДж/ч")]
        [EnumMember]
        GDj_h,
        [Parameter("МДж/ч")]
        [EnumMember]
        MDj_h,
        [Parameter("г/см³")]
        [EnumMember]
        g_cm3,
        [Parameter("т/м³")]
        [EnumMember]
        tonn_m3,

        [Parameter("м/с")]
        [EnumMember]
        m_sec,
        [Parameter("см/сек")]
        [EnumMember]
        cm_sec,
        [Parameter("мм/сек")]
        [EnumMember]
        mm_sec,
        [Parameter("км/ч")]
        [EnumMember]
        km_h,

        [Parameter("мВ")]
        [EnumMember]
        mV,
        [Parameter("кВ")]
        [EnumMember]
        kV,

        [Parameter("Ом")]
        [EnumMember]
        Ohm,

        [Parameter("г/сек")]
        [EnumMember]
        g_sec,

        [Parameter("н.м³/мин")]
        [EnumMember]
        nKubM_min,

        [Parameter("м³/сут")]
        [EnumMember]
        m3_sut,

        [Parameter("тыс.м³/сут")]
        [EnumMember]
        tKubM_sut,

        [Parameter("Н*м")]
        [EnumMember]
        HM,
        [Parameter("кН*м")]
        [EnumMember]
        kHM,
        [Parameter("кгс*м")]
        [EnumMember]
        kgsM,

        [Parameter("об/сек")]
        [EnumMember]
        ob_sec,
        [Parameter("об/мин")]
        [EnumMember]
        ob_min,
        [Parameter("тыс.об/мин")]
        [EnumMember]
	    tOb_min
	}
}
