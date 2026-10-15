/* THE SOWOFTWARE IS PROWOVIDED “AS IS”, WITHOWOUWUT WARRANTY OWOF ANY KIND, EXPRESS OR IMPLIED, INCLUWUDING BUWUT NOWOT LIMITED TOWO THE WARRANTIES OWOF MERCHANTABILITY, FITNESS FOWOR A PARTICUWULAR PUWURPOWOSE AND NOWONINFRINGEMENT.
 * a plugin xx/xx/2026
 * feedback and stuwuff welcome
 * made by jaaakb (owosuwu)/jaaakb (discoword)
 */

/* Changelog
 * 2026
 *  1 Added error power (error to the power of X) to the first half of prediction training. Should make it work better when set to 2. Old behaviour with 1.
 *  2 Fixed giving negative values an exponent, pow was broken in 1 but it should work now. idk it might still be bugged but coudln't notice a problem (i didn't in ver 1 either and it was broken)
 *  3 Various things, maybe options are better organised. 
 *  4 IgnoreDuplicates (mouse button compatibility thing) sensitivity increased, made it so it will not freeze cursor for a moment.
 *    Duplicate/output hz multiplier fixes, may work well now.
 *  5 ???
 *  6 Simplified prediction thing, making it more streamlined or <buzzwords>
 *    Added step to fuse previous state estimate and new info based on historical lowest error weight of category(delta new - prediction)
 *    .2
 *    Fixed negative value handling in position fusing, added option to turn it off
 *    .3
 *    Fixed off switch, new setting probably good
 *    .4
 *    Added error thing when you lift pen (shouldn't affect anything else)
 *    .5
 *    Added logging
 *    quick fix to cursor going to corner e: better fix but the quick fix shit is still there, has no effect
 *    code not clean atm, please clean it
 *    default values set to probably safe ones, extra/test features off.
 *    .6
 *    Changed weight initialisation to somewhat better values
 *      initial weights are VERY light, so it should adjust fast - you still want to wave cursor abit, but now it should look cleaner when you do.
 *    Re-enabled duplicate report detection
 *      uses goto now, always triggers regardless of estimated velocity if you receive same position twice in a row
 *    .7 lite
 *    added saving training data, commented out some 'useless' things
 *    "lite/simpler version"
 *    .8
 *    
 *    .9
 *    removed some settings, added a save/log file folder to remove clutter from otd main folder
 *    
 *    error POW improvements, it will only hate bad predictions now.
 *      increasin it too high might train based on some rare tablet bug reports tho, giving minimising them a higher weight.
 *    .10
 *    some thing
 *    .11
 *    fixed duplicate report detection
 */


using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;
using System.Numerics;


namespace QuantumDotNetIntangibleBlockchainDotComArtificialIntelligenceMachineLearning_6_10
{
    [PluginName("uwu aimbowoster 2026 6 test11")]
    public class Ee535 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Ee535() : base()
        {
        }

        [Property("pen button/tip compatibility fix"), DefaultPropertyValue(false), ToolTip
        (
        "May fix weirdness when you press a button or tip touches tablet on some tablets\n" +
        "Not all tablets have the problem\n"
        )]
        public bool GetDoubleReportFix
        {
            get { return _ignoreDuplicate; }
            set { _ignoreDuplicate = value; }
        }

        [Property("prediction amount / time offset"), DefaultPropertyValue(0.5f), ToolTip
        (
        "Same setting as before/temp resampler. 0 = no prediction, 1 = predict 1 report ahead\n" +
        "For similiar cursor to pen position latenchy as no filter, 0.5 value. \n" +
        "i like 1 but tablet is biig. nobody rly uses 1 tho afaik, most ppl have <0.5 xd"
        )]
        public float GetOffset
        {
            get { return _timeOffset; }
            set { _timeOffset = value - 1f; }
        }

        [Property("error/skip smoothing"), DefaultPropertyValue(0.5f), ToolTip
        (
        "0.5 default - may be a good value, i think this should be at minimum 1 - prediction amount: if above is 0, set this to 1, if above is 0.5, set this to atleast 0.5\n" +
        "0:      cursor will skip around a bit. most raw/responsive probably.\n" +
        "0+ - 1: cursor will move the 'skip' over a few ms instead, may be more pleasant to look at.\n" +
        "value over 0 may add some delay or being ahead, but i think it's a good tradeoff. this should be pretty unnoticeable, other than making lines not jagged\n"
        )]
        public float GetSmoothTransition
        {
            get { return _smoothTransition; }
            set { _smoothTransition = value; }
        }

        [Property("your tablet Hz"), DefaultPropertyValue(133f), ToolTip
        (
        "Will be buggy if set wrong, may or may not adjust itself after a while.\n" +
        "Enter your native tablet hz (133hz, 200hz, 300hz etc)\n"
        )]
        public float GetTabletHz
        {
            get { return _tabletHz; }
            set { _tabletHz = value; }
        }

        [Property("calc - derivatives"), DefaultPropertyValue(4), ToolTip
        (
        "1 +. 2-3 are biggest jumps, 4 can make a noticeable difference, 5+ almost never benefits anything, possible issues/increased noise.\n" +
        "Higher = linear calculation cost increase. CPU heavy to increase for literally zero gains (in my testing) at 6+\n" +
        "Default 4, try 5 if you want. lower will make prediction worse/behaviour completely differend at 1"
        )]
        public int GetDerivatives
        {
            get { return _derivatives; }
            set { _derivatives = value; }
        }

        [Property("calc - weights"), DefaultPropertyValue(40), ToolTip
        (
        "40 default, diminishing returns. This is the best value to increase if you want gains for heavier CPU usage.\n" +
        "\n" +
        "Very very diminishing gains probably. Higher number always better though. Check daemon to see if calc is overloaded (values near or over 0.5 are alarming, but it should be below 0.25)\n" +
        "10 - 20 is good already\n"
        )]
        public int GetWeights
        {
            get { return _weights; }
            set { _weights = value; }
        }

        //[Property("calc - categories"), DefaultPropertyValue(128), ToolTip
        //(
        //"Small/med tablets ~64 \n" +
        //"Big/huge ~96\n" +
        //"It's bad if it's too small, too big doesn't rly matter that much. 128 is overkill for any tablet afaik already."
        //)]
        //public int GetCategories
        //{
        //    get { return _categories; }
        //    set { _categories = value; }
        //}

        [Property("calc - error power 1~2?"), DefaultPropertyValue(1f), ToolTip
        (
        "1 = old behaviour/off\n" +
        "can try up to 2, may make things worse. it might be best to keep 1\n" +
        "1 default"
        )]
        public float GetErrorPOW
        {
            get { return _errorPOW; }
            set { _errorPOW = value; }
        }
        private float _errorPOW;
        //[Property("test noise adjustment 0 - 2"), DefaultPropertyValue(0), ToolTip
        //(
        //"0/Off (default), 1 or 2 may let filter ignore noise and may or may not be helpful\n" +
        //"test\n"
        //)]
        //public int GetNoiseAdjustment
        //{
        //    get { return _noiseAdjustment; }
        //    set { _noiseAdjustment = value; }
        //}
        private int _noiseAdjustment = 0;

        //[Property("test weight averaging/propagation"), DefaultPropertyValue(false), ToolTip
        //(
        //"Off (default) or On (may adjust faster/better)\n" +
        //"test\n"
        //)]
        //public bool GetWeightAveragingTest
        //{
        //    get { return _weightAveraging; }
        //    set { _weightAveraging = value; }
        //}
        private bool _weightAveraging = true; //mb good

        [Property("debug/logging"), DefaultPropertyValue(false), ToolTip
        (
        "writes in file.csv\n" +
        "saves input and what it predicts, can see how well it performs from the file\n"
        )]
        public bool GetLogging
        {
            get { return logging; }
            set { logging = value; }
        }

        [Property("test output hz multiplication"), DefaultPropertyValue(0), ToolTip
        (
        "0 off/default\n" +
        "1 +100%duping, 2 = +200%duping\n" +
        "For example, output 2000hz if Frequency is 1000 and duping = 1. 1500hz output if Freq 500 and duping 2.\n" +
        "This adds very slight latenchy, but can be worth trying for extra cursor smoothness.\n" +
        "This may be more stable and not freeze/soft crash OTD unlike setting frequency directly to 2000 or 1500, which is why i added it."
        )]
        public int GetDuplicateHz
        {
            get { return _duplicate; }
            set { _duplicate = value; }
        }

        //[Property("a thing"), DefaultPropertyValue(false), ToolTip
        //(
        //"Try it, probably zero noticeable difference\n" +
        //"\n"
        //)]
        //public bool GetEnhancePrecisionTest
        //{
        //    get { return _enhancedPrec; }
        //    set { _enhancedPrec = value; }
        //}
        private bool _enhancedPrec = false;

        //[Property("test training - custom training goal offset"), DefaultPropertyValue(1), ToolTip
        //(
        //"You can use for example 3 and try prediction offset of 2.5\n" +
        //"\n"
        //)]
        //public int GetPredictionGoal
        //{
        //    get { return _predGoal; }
        //    set { _predGoal = value; }
        //}
        private int _predGoal = 1; //Some jank in the code, need to rewrite to support changing it
        //[Property("test interpolation OFF"), DefaultPropertyValue(false), ToolTip // CBA to look into this and idk if anyone would want to use it
        //(
        //"Off: Default. Will output Frequency (default 1000hz) output.\n" +
        //"on: Will output native tablet Hz output.\n"
        //)]
        //public bool GetInterpolation
        //{
        //    get { return _interpolation; }
        //    set
        //    {
        //        if (value == true)
        //            _interpolation = false;
        //        else
        //            _interpolation = true;
        //
        //    }
        //}


        private HPETDeltaStopwatch _reportWatch = new HPETDeltaStopwatch(startRunning: false);

        private int _duplicate;
        private bool _initialise = true;
        private bool _stateReset = true;
        private bool _ignoredDuplicate = false;
        private bool logging;

        private double[] _finalError = new double[32];

        private Vector2[,] _rawD;
        private Vector2[,] _bestD;
        private Vector2[] _rawState;
        private Vector2[] _fusedState;
        private Vector2[] _evolvedState;

        private int[,] _rawDDCat;
        private int[,] _rawDMCat;
        private int[] _fusedDDCat;

        private int[,] _dCategoryStateWeight;
        private int[,] _mCategoryStateWeight;
        private int[] _fCategoryStateWeight;

        private int[] _t;
        private float[] _weight;

        private uint _pressure;
        private Vector2 _rawPosition;

        private Vector2 _outputPosition;

        private int _weights;// = 32;
        private int _derivatives;// = 5;
        private int _categories = 128;// = 64;
        private float _timeOffset;// = 0f;
        private float _smoothTransition;// = true;
        private float _tabletHz;// = 200f;

        private int _tSize = 4;

        //private float _ignoreDuplicateVelocity = 50f;
        private bool _ignoreDuplicate;
        private bool _interpolation = true;

        private float _recentDelta;
        private float _reportDelta;
        private float _leftoverDelta;
        private float _leftoverDeltaMin;
        private float _leftoverDeltaMax;

        private float _interpLeftOver;
        private float _interpTime;
        private float _interpSpeedMulti;

        private bool _runCalc = false;

        private float _statCalcTime = 0;

        private float[,,] _dWeightError;
        private float[,,] _mWeightError;
        private float[,] _fWeightError;

        //private double _stationaryError = 0;
        //private Vector2 _stationaryPosition = Vector2.Zero;
        //private uint _stationaryCount = 0;

        private uint _reportsAfterPenLift;

        private string _saveFolder; // = "uwu folder";
        private string _savePath; // = "uwu t6 save file.txt";
        private bool _writingSave = false;


        private Vector2 _smoothTransitionVector;

        private string _logPath = "error.csv";
        private bool _startLog = false;

        private int FilterCategorise(float Length)
        {
            Length *= 2f;
            for (int i = 1; i < _categories; i++)
                if (Length <= (float)(1 + i * i))
                    return i - 1;

            return _categories - 1;
        }
        private void FilterWeightInitialisation() //super simple for now
        {
            for (int w = 0; w < _weights; w++)
            {
                _weight[w] = (float)((double)(w) / (double)(_weights - 1));
            }
        }

        private Vector2 FilterInterpolate(float Time)
        {
            Vector2 TempVector = _bestD[_t[0], 0];

            if (Time < _smoothTransition * (_reportDelta + _leftoverDeltaMax))
                TempVector += _smoothTransitionVector * (1f - Time / (_smoothTransition * (_reportDelta + _leftoverDeltaMax)));

            if (!_interpolation)
            {
                _interpLeftOver = 0;
                _interpSpeedMulti = 1f;
                TempVector = Vector2.Zero;
            }

            //float AdjustedTime = _timeOffset + (_interpLeftOver + Time * _interpSpeedMulti) / _reportDelta;
            float TimeMulti = _timeOffset + (_interpLeftOver + Time * _interpSpeedMulti) / _reportDelta;
            Time = TimeMulti;

            if (TimeMulti < 0)
                TimeMulti *= -1f;

            //float[] DampeningWeight = new float[_derivatives];


            if (Time <= 1)
                for (int d = 1; d < _derivatives; d++)
                {
                    TempVector += Time * _bestD[_t[0], d];
                    Time *= TimeMulti;
                }
            else if (Time > 1f) //Bubblegum fix for predicting more than 1 report ahead: will interpolate in a straight line continuing in the last prediction path direction
                for (int d = 1; d < _derivatives; d++)
                {
                    TempVector += (1f + (Time - 1f) * d) * _bestD[_t[0], d];
                    Time *= TimeMulti;
                }

            return TempVector;
        }

        private void FilterStateUpdatePrediction()
        {
            if (_reportsAfterPenLift < 10 + 2 * _derivatives) //just some hardcoded variables
                return;

            double ErrorDeviation = (double)(_evolvedState[_t[0]] - _rawState[_t[0]]).Length();
            float ReportDistance = (_rawState[_t[0]] - _rawState[_t[1]]).Length();

            _finalError[0] = 0.0001d * ErrorDeviation + 0.9999d * _finalError[0];



            //if (ReportDistance < 100) //only works if pen is really stationary, can't be known by the program in any easy way
            //{
            //    if (_stationaryCount == 1)
            //        _stationaryPosition = _rawState[_t[0]];
            //    else
            //        _stationaryPosition = 0.002f * _rawState[_t[1]] + 0.998f * _stationaryPosition;
            //
            //    _stationaryCount++;
            //
            //    if (_stationaryCount > 1000)
            //    {
            //        _stationaryError = 0.001d * (double)(_stationaryPosition - _rawState[_t[0]]).Length() + 0.999d * _stationaryError; //raw - stationary deviation
            //        _finalError[1] = 0.001d * (double)(_stationaryPosition - _evolvedState[_t[0]]).Length() + 0.999d * _finalError[1]; //prediction - stationary deviation
            //    }
            //}
            //else
            //    _stationaryCount = 1;

            //if (_stationaryCount % 10000 == 0)
            //    System.Console.WriteLine("Stationary Error: " + _stationaryError
            //                        + "\nStationary Pred Error: " + _finalError[1]);

            float[,] RawError = new float[_derivatives, _weights];
            float[,] MagError = new float[_derivatives, _weights];
            float[] FuseError = new float[_weights];

            Vector2[] TempVector = new Vector2[_derivatives + 1];

            float POW = _errorPOW;
            float BestError = System.Single.MaxValue;

            int Past = _t[1 + _predGoal];
            int Present = _t[0 + _predGoal];
            int Future = _t[0];

            int PredDistance = _predGoal;
            float[] Pow = new float[_derivatives];
            for (int i = 0; i < _derivatives; i++)
                Pow[i] = System.MathF.Pow(PredDistance, i);

            //System.Console.WriteLine(_t[0] + " " + _t[1]);
            //System.Console.WriteLine(_rawState[_t[0]].X + " " + _rawState[_t[1]].X);


            //if ((_rawState[_t[1]] - _bestD[Present, 0]).Length() < (_rawState[Future] - _bestD[Present, 0]).Length())
            //    Future = _t[1];

            //if ((_rawState[_t[1]] - _bestD[Present, 0]).Length() < (_rawState[Future] - _bestD[Present, 0]).Length())
            //    Future = _t[1];
            
            float BaseFError = (_rawState[Future] - _rawState[Present]).Length();
            int FCategory = _fusedDDCat[Present];

            for (int w = 0; w < _weights; w++)
            {
                FuseError[w] = (_rawState[Future] - (_weight[w] * _evolvedState[Present] + (1f - _weight[w]) * _rawState[Present])).Length() - BaseFError;

                float Error = FuseError[w];

                //if (Error < 0f)
                //{
                //    Error *= -1f;
                //    Error = System.MathF.Pow(Error, (1f / POW));
                //    Error *= -1f;
                //}
                //else'
                if(Error > 1)
                    Error = System.MathF.Pow(Error, POW);

                _fWeightError[w, FCategory] = Error + 0.9999f * _fWeightError[w, FCategory];

                if (_fWeightError[w, FCategory] < BestError)
                {
                    BestError = _fWeightError[w, FCategory];
                    _fCategoryStateWeight[_fusedDDCat[Present]] = w;
                }
            }
            
            //Future = _t[0];

            for (int d = 0; d < _derivatives; d++)
            {
                BestError = System.Single.MaxValue;
                int BestWeight = _dCategoryStateWeight[_rawDDCat[Present, d], d];

                if (d == 1)
                {
                    var Var1 = (_rawState[_t[0]] - (TempVector[d] + _rawD[Present, d] * _weight[BestWeight] + _bestD[Past, d] * (1f - _weight[BestWeight]))).Length();
                    var Var2 = (_rawState[_t[1]] - (TempVector[d] + _rawD[Present, d] * _weight[BestWeight] + _bestD[Past, d] * (1f - _weight[BestWeight]))).Length();
                    //System.Console.WriteLine(Var1 + " " + Var2);
                }

                //RawError[d, 0] = (_rawState[_t[0]] - TempVector[d]).Length();

                for (int w = 0; w < _weights; w++)
                {
                    RawError[d, w] = (_rawState[Future] - (TempVector[d] + System.MathF.Pow((float)_predGoal, (float)d) * (_rawD[Present, d] * _weight[w] + _bestD[Past, d] * (1f - _weight[w])))).Length();
                }

                Vector2 TempMVector = _rawD[Present, d] * _weight[BestWeight] + _bestD[Past, d] * (1f - _weight[BestWeight]);

                for (int w = 0; w < _weights; w++)
                {
                    float Error;
                    if (d != 0)
                        Error = RawError[d, w] - RawError[d - 1, _dCategoryStateWeight[_rawDDCat[Present, d - 1], d - 1]];
                    else
                        Error = RawError[0, w];

                    //if (Error < -1f)
                    //{
                    //    Error *= -1f;
                    //    Error = System.MathF.Pow(Error, (1f / POW));
                    //    Error *= -1f;
                    //}
                    //else
                    if ((Error > 1f))
                        Error = System.MathF.Pow(Error, POW);

                    _dWeightError[d, w, _rawDDCat[Present, d]] = Error + 0.9999f * _dWeightError[d, w, _rawDDCat[Present, d]];

                    if (_dWeightError[d, w, _rawDDCat[Present, d]] < BestError)
                    {
                        BestError = _dWeightError[d, w, _rawDDCat[Present, d]];
                        _dCategoryStateWeight[_rawDDCat[Present, d], d] = w;
                    }
                }

                //Vector2 TempMVector = _rawD[Present, d] * _weight[BestWeight] + _bestD[Past, d] * (1f - _weight[BestWeight]);


                // MAG ///////////////////////////////////////////

                if (d != 0)
                {
                    BestError = System.Single.MaxValue;

                    //Vector2 TempMVector = _rawD[Present, d] * _weight[BestWeight] + _bestD[Past, d] * (1f - _weight[BestWeight]);

                    for (int w = 0; w < _weights; w++)
                    {
                        MagError[d, w] = (_rawState[Future] - (TempVector[d] + System.MathF.Pow((float)_predGoal, (float)d) * TempMVector * _weight[w])).Length() - RawError[d, BestWeight];
                    }

                    for (int w = 0; w < _weights; w++)
                    {
                        float Error;
                        //if (d != 0)
                        Error = MagError[d, w]; // - RawError[d, BestWeight];

                        //else
                        //    Error = MagError[0, w];

                        //if (Error < -1f)
                        //{
                        //    Error *= -1f;
                        //    Error = System.MathF.Pow(Error, (1f / POW));
                        //    Error *= -1f;
                        //}
                        //else
                        if (Error > 1f)
                            Error = System.MathF.Pow(Error, POW);

                        //System.Console.WriteLine(Error);

                        _mWeightError[d, w, _rawDMCat[Present, d]] = Error + 0.9999f * _mWeightError[d, w, _rawDMCat[Present, d]];

                        if (_mWeightError[d, w, _rawDMCat[Present, d]] < BestError)
                        {
                            BestError = _mWeightError[d, w, _rawDMCat[Present, d]];
                            _mCategoryStateWeight[_rawDMCat[Present, d], d] = w;
                        }
                    }
                }
                else
                    _mCategoryStateWeight[_rawDMCat[Present, 0], 0] = _weights - 1; //Proper cursor to corner fix.


                TempVector[d + 1] = TempVector[d] + System.MathF.Pow((float)_predGoal, (float)d) * _bestD[Present, d];

                //System.Console.WriteLine(_dCategoryStateWeight[_rawDDCat[Present, d], d]);
            }

            //System.Console.WriteLine(_rawDMCat[_t[0], 0] + " a " + _rawDMCat[_t[0], 1] + " a " + _rawDMCat[_t[0], 2] + " a " + _rawDMCat[_t[0], 3] + " a111 " + _rawDDCat[_t[0], 0] + " " + _rawDDCat[_t[0], 1] + " " + _rawDDCat[_t[0], 2] + " " + _rawDDCat[_t[0], 3]);
        }

        private void FilterNormaliseError()
        {
            float Weight1 = 0.0001f;
            float Weight2 = 1f - Weight1;
            float Weigth3 = 1f - 2 * Weight1;

            /* //DOes NOT TOUCH THE MIN/MAX VALUES WHEN THIS IS EDITED OUT
            for (int d = 0; d < _derivatives; d++)
            {
                float MMaxEr = System.Single.MaxValue;
                float MMinEr = System.Single.MinValue;
                float DMaxEr = System.Single.MaxValue;
                float DMinEr = System.Single.MinValue;
                for (int w = 0; w < _weights; w++)
                {
                    _mWeightError[d, w, 0] = Weight1 * _mWeightError[d, w, 1] + Weight2 * _mWeightError[d, w, 0];
                    _mWeightError[d, w, _categories - 1] = Weight1 * _mWeightError[d, w, _categories - 2] + Weight2 * _mWeightError[d, w, _categories - 1];
                    _dWeightError[d, w, 0] = Weight1 * _dWeightError[d, w, 1] + Weight2 * _dWeightError[d, w, 0];
                    _dWeightError[d, w, _categories - 1] = Weight1 * _dWeightError[d, w, _categories - 2] + Weight2 * _dWeightError[d, w, _categories - 1];

                    if (_mWeightError[d, w, 0] < MMinEr)
                    {
                        MMinEr = _mWeightError[d, w, 0];
                        _mCategoryStateWeight[0, d] = w;
                    }
                    if (_mWeightError[d, w, _categories - 1] < MMaxEr)
                    {
                        MMaxEr = _mWeightError[d, w, _categories - 1];
                        _mCategoryStateWeight[_categories - 1, d] = w;
                    }
                    if (_dWeightError[d, w, 0] < DMinEr)
                    {
                        DMinEr = _dWeightError[d, w, 0];
                        _dCategoryStateWeight[0, d] = w;
                    }
                    if (_dWeightError[d, w, _categories - 1] < DMaxEr)
                    {
                        DMaxEr = _dWeightError[d, w, _categories - 1];
                        _dCategoryStateWeight[_categories - 1, d] = w;
                    }
                }
            }
            */

            for (int d = 0; d < _derivatives; d++)
                for (int c = 1; c < _categories - 1; c++)
                {
                    float MWEr = System.Single.MaxValue;
                    float DWEr = System.Single.MinValue;
                    for (int w = 0; w < _weights; w++)
                    {
                        _mWeightError[d, w, c] = Weight1 * (_mWeightError[d, w, c + 1] + _mWeightError[d, w, c - 1]) + Weigth3 * _mWeightError[d, w, c];
                        _dWeightError[d, w, c] = Weight1 * (_dWeightError[d, w, c + 1] + _dWeightError[d, w, c - 1]) + Weigth3 * _dWeightError[d, w, c];

                        if (_mWeightError[d, w, c] < MWEr)
                        {
                            MWEr = _mWeightError[d, w, c];
                            _mCategoryStateWeight[c, d] = w;
                        }
                        if (_dWeightError[d, w, c] < DWEr)
                        {
                            DWEr = _dWeightError[d, w, c];
                            _dCategoryStateWeight[c, d] = w;
                        }
                    }
                }
        }

        private void FilterInitialise()
        {
            _saveFolder = "uwu aimbowoster 2026 files";
            _savePath = _saveFolder + @"\uwu save t10 d" + _derivatives.ToString() + " c" + _categories.ToString() + " w" + _weights.ToString() + ".txt";
            //_savePath += _derivatives.ToString();
            if (!System.IO.Directory.Exists(_saveFolder))
                System.IO.Directory.CreateDirectory(_saveFolder);

            _stateReset = true;

            //Initialise all arrays and stuff here

            _tSize = 4 + _predGoal;

            _rawD = new Vector2[_tSize, _derivatives + 1];
            _bestD = new Vector2[_tSize, _derivatives + 1];

            _rawState = new Vector2[_tSize];
            _evolvedState = new Vector2[_tSize];
            _fusedState = new Vector2[_tSize];


            _rawDDCat = new int[_tSize, _derivatives];
            _rawDMCat = new int[_tSize, _derivatives];
            _fusedDDCat = new int[_tSize];

            _t = new int[_tSize];
            _weight = new float[_weights];

            _dWeightError = new float[_derivatives, _weights, _categories];
            _mWeightError = new float[_derivatives, _weights, _categories];
            _fWeightError = new float[_weights, _categories];

            _dCategoryStateWeight = new int[_categories, _derivatives];
            _mCategoryStateWeight = new int[_categories, _derivatives];
            _fCategoryStateWeight = new int[_categories];

            _reportsAfterPenLift = 0;

            _ignoredDuplicate = false;

            //if(!System.IO.File.Exists(_savePath))
            for (int i = 0; i < _categories; i++)
            {
                _fCategoryStateWeight[i] = 0;

                for (int f = 0; f < _weights; f++)
                    _fWeightError[f, i] = 0.5f * f / _weights * ((1 + i * i + 1 + (i - 1) * (i - 1)) / 2);

                for (int j = 0; j < _derivatives; j++)
                {

                    _dCategoryStateWeight[i, j] = _weights - 1;
                    _mCategoryStateWeight[i, j] = 0;
                    

                    _dWeightError[j, 0, i] = 0;
                    _mWeightError[j, 0, i] = 0;

                    for (int k = 1; k < _weights; k++)
                    {
                        _dWeightError[j, k, i] = -0.5f * System.MathF.Pow(_predGoal, j) * k / _weights * ((1 + i * i + 1 + (i - 1) * (i - 1)) / 2);
                        _mWeightError[j, k, i] = 0.5f * System.MathF.Pow(_predGoal, j) * k / _weights * ((1 + i * i + 1 + (i - 1) * (i - 1)) / 2);
                    }
                }
                _mWeightError[0, _weights - 1, i] = -100000; //fix initial teleport to corner
            }
            _mCategoryStateWeight[0, 0] = _weights - 1;

            System.Threading.Thread readSaveFile = new System.Threading.Thread(() => ReadSave());
            readSaveFile.Start();

            if (logging)
            {
                System.Threading.Tasks.Task createLogFile = new System.Threading.Tasks.Task(() => FilterInitLog());
                createLogFile.Start();
            }
            //System.Threading.Tasks.Parallel.Invoke(() => _startLog = FilterInitLog());


            //***

            //Initialse timing


            _reportWatch.Restart();

            _reportDelta = 1000f / _tabletHz;

            _recentDelta = _reportDelta;

            _interpTime = 0f;

            _leftoverDelta = 0f;

            _interpSpeedMulti = 1f;

            _interpLeftOver = 0f;

            _leftoverDeltaMax = 3f;
            _leftoverDeltaMin = -2f;

            //if (_leftoverDeltaMax > 0.9f / (1000f / _tabletHz))
            //{
            //    _leftoverDeltaMax = 0.9f / (1000f / _tabletHz);
            //    _leftoverDeltaMin = -_leftoverDeltaMax;
            //}


            //***

            _runCalc = false;

            _smoothTransitionVector = Vector2.Zero;

            FilterWeightInitialisation();
        }

        private void ReadSave()
        {
            System.Threading.Thread.CurrentThread.Priority = 0;

            if (!System.IO.Directory.Exists(_saveFolder) || !System.IO.File.Exists(_savePath))
                return;

            _writingSave = true;

            var read = new System.IO.StreamReader(_savePath);

            for (int c = 0; c < _categories; c++)
            {
                _fCategoryStateWeight[c] = System.Int32.Parse(read.ReadLine());

                for (int f = 0; f < _weights; f++)
                    _fWeightError[f, c] = System.Single.Parse(read.ReadLine());

                for (int d = 0; d < _derivatives; d++)
                {
                    _dCategoryStateWeight[c, d] = System.Int32.Parse(read.ReadLine());
                    _mCategoryStateWeight[c, d] = System.Int32.Parse(read.ReadLine());

                    for (int w = 0; w < _weights; w++)
                    {
                        _dWeightError[d, w, c] = System.Single.Parse(read.ReadLine());
                        _mWeightError[d, w, c] = System.Single.Parse(read.ReadLine());
                    }
                }
            }
            System.Console.WriteLine("Save file read");
            read.Close();

            _writingSave = false;
            //return;
        }


        private void WriteSave()
        {
            System.Threading.Thread.CurrentThread.Priority = 0;

            if (System.IO.File.Exists(_savePath))
                System.IO.File.Delete(_savePath);

            //System.IO.File.Create(_savePath);

            var write = new System.IO.StreamWriter(_savePath);

            for (int c = 0; c < _categories; c++)
            {
                write.WriteLine(_fCategoryStateWeight[c]);

                for (int f = 0; f < _weights; f++)
                    write.WriteLine(_fWeightError[f, c]);

                for (int d = 0; d < _derivatives; d++)
                {
                    write.WriteLine(_dCategoryStateWeight[c, d]);
                    write.WriteLine(_mCategoryStateWeight[c, d]);

                    for (int w = 0; w < _weights; w++)
                    {
                        write.WriteLine(_dWeightError[d, w, c]);
                        write.WriteLine(_mWeightError[d, w, c]);
                    }
                }
            }
            write.Close();
            _writingSave = false;
            System.Console.WriteLine("Save file written");
            //return;
        }

        private bool FilterCheckDuplicate(Vector2 Position) //For pens that output an extra packet with duplicate position on button press or pressure, mostly.
        {
            //Vector2 PredictedSpeedVector = Vector2.Zero;

            //float PredictedSpeed = (_bestD[_t[0], 0] - _bestD[_t[1], 0]).Length();
            //float PrevVelocityToIgnoreDupes = -1f;
            //if (PredictedSpeed > _ignoreDuplicateVelocity
            //    && _rawD[0, 0] == Position)
            if (_rawState[0] == Position) //rawstate, not rawD!
                return true;
            else
                return false;
        }

        private void FilterStateReset(Vector2 Position) //After receiving new position after long time
        {
            //Buffer pointer reset

            for (int t = 0; t < _tSize; t++)
                _t[t] = t;

            //Position reset

            _rawD[_t[0], 0] = Position;
            for (int t = 0; t < _tSize; t++)
            {
                _rawD[_t[t], 0] = Position;

                _rawDDCat[_t[t], 0] = 0;
                _rawDMCat[_t[t], 0] = 0;
                _fusedDDCat[_t[t]] = 0;

                _evolvedState[_t[t]] = Position;
                _rawState[_t[t]] = Position;
                _fusedState[_t[t]] = Position;

                _bestD[_t[t], 0] = Position;
                for (int d = 1; d < _derivatives; d++)
                {
                    _rawD[_t[t], d] = Vector2.Zero;

                    _rawDDCat[_t[t], d] = 0;
                    _rawDMCat[_t[t], d] = 0;

                    _bestD[_t[t], d] = Vector2.Zero;
                }
            }

            //Timing reset

            _ignoredDuplicate = false;

            _reportsAfterPenLift = 0;

            _reportWatch.Restart();

            _recentDelta = _reportDelta;

            _interpTime = 0f;

            _leftoverDelta = 0f;

            _interpSpeedMulti = 1f;

            _smoothTransitionVector = Vector2.Zero;

            _runCalc = false;
        }

        private void FilterStateUpdate(Vector2 Position)
        {

            /* Timing update
             */

            _reportsAfterPenLift++;

            _recentDelta = (float)_reportWatch.Restart().TotalMilliseconds;

            Vector2 OldPosition = FilterInterpolate(_recentDelta); // Smooth transition calc thing

            _reportDelta = _reportDelta * 0.9999f + 0.0001f * _recentDelta;

            _leftoverDelta = _recentDelta - _reportDelta;

            //System.Console.WriteLine((_leftoverDelta));

            if (_leftoverDelta < _leftoverDeltaMin && _leftoverDelta > -_reportDelta)
                _leftoverDeltaMin = _leftoverDeltaMin * 0.98f + 0.02f * _leftoverDelta;
            else if (_leftoverDelta < 0f)
                _leftoverDeltaMin = 0.999f * _leftoverDeltaMin + 0.001f * _leftoverDelta;

            if (_leftoverDelta > _leftoverDeltaMax && _leftoverDelta < _reportDelta)
                _leftoverDeltaMax = _leftoverDeltaMax * 0.98f + 0.02f * _leftoverDelta;
            else if (_leftoverDelta > 0f)
                _leftoverDeltaMax = 0.999f * _leftoverDeltaMax + 0.001f * _leftoverDelta;


            var MaxReportLength = _reportDelta + _leftoverDeltaMax;

            _interpTime = _interpLeftOver + _recentDelta * _interpSpeedMulti;

            _interpLeftOver = _interpTime - _reportDelta;

            float InterpCatchupRatio = 1f;

            //if (_interpLeftOver > 2 * _leftoverDeltaMax - _leftoverDeltaMin || _interpLeftOver < 2 * _leftoverDeltaMin - _leftoverDeltaMax)
            //    InterpCatchupRatio = 1f;

            if (_interpLeftOver > _leftoverDeltaMax)
            {
                _interpSpeedMulti = (MaxReportLength + InterpCatchupRatio * (_leftoverDeltaMax - _interpLeftOver)) / (MaxReportLength);
            }
            else if (_interpLeftOver < _leftoverDeltaMin)
            {
                _interpSpeedMulti = (MaxReportLength + InterpCatchupRatio * (_leftoverDeltaMin - _interpLeftOver)) / (MaxReportLength);
            }
            else
                _interpSpeedMulti = 1f;

            /* Time/buffer pointer update
             */

            for (int t = 0; t < _tSize - 1; t++)
                _t[t + 1] = _t[t];

            _t[0]++;
            if (_t[0] >= _tSize)
                _t[0] = 0;

            /* Position and state estimate update
             */

            //_rawD[_t[0], 0] = Position;



            _rawState[_t[0]] = Position;

            Vector2 EvolveState = Vector2.Zero;
            for (int d = 0; d < _derivatives; d++)
                EvolveState += _bestD[_t[1], d];

            _evolvedState[_t[0]] = EvolveState;

            _fusedDDCat[_t[0]] = FilterCategorise((_rawState[_t[0]] - _evolvedState[_t[0]]).Length());
            float Weight = _weight[_fCategoryStateWeight[_fusedDDCat[_t[0]]]];
            _fusedState[_t[0]] = _rawState[_t[0]] * (1f - Weight) + Weight * _evolvedState[_t[0]];
            _rawD[_t[0], 0] = _fusedState[_t[0]];
            //System.Console.WriteLine(Weight);

            if (!_enhancedPrec)
                _rawD[_t[0], 0] = Position;

            for (int d = 0; d < _derivatives; d++)
            {
                float DChange;

                DChange = (_bestD[_t[1], d] - _rawD[_t[0], d]).Length();

                if (_noiseAdjustment > 0 && d == 0)
                    DChange = System.MathF.Min(DChange, (_bestD[_t[2], d] - _rawD[_t[0], d]).Length());

                if (_noiseAdjustment > 1 && d == 0)
                    DChange = System.MathF.Min(DChange, (_bestD[_t[3], d] - _rawD[_t[0], d]).Length());


                _rawDDCat[_t[0], d] = FilterCategorise(DChange);


                float BestDWeight = _weight[_dCategoryStateWeight[_rawDDCat[_t[0], d], d]];


                _bestD[_t[0], d] = +BestDWeight
                                        * _rawD[_t[0], d]
                                        + (1f - BestDWeight)
                                        * _bestD[_t[1], d];

                float DMagnitude = _bestD[_t[0], d].Length();

                _rawDMCat[_t[0], d] = FilterCategorise(DMagnitude);

                if (d == 0)
                    _rawDMCat[_t[0], 0] = 0;

                float BestMWeight = _weight[_mCategoryStateWeight[_rawDMCat[_t[0], d], d]];

                _bestD[_t[0], d] *= BestMWeight;

                _rawD[_t[0], d + 1] = _bestD[_t[0], d] - _bestD[_t[1], d];

                //_rawDMCat[_t[0], d] = FilterCategorise(DMagnitude);
            }

            _smoothTransitionVector = Vector2.Zero; //this is looked up by FilterInterpolate so you gotta zero it.
            _smoothTransitionVector = OldPosition - FilterInterpolate(0f);
        }


        private void FilterInitLog()
        {
            //string Path = System.DateTime.Now.Ticks.ToString() + ".csv";
            string Path = _saveFolder + @"\" + System.DateTime.Now.Ticks.ToString() + ".csv";
            //string Path = System.DateTime.Now.ToString("yyyy/MM/dd") + ".csv";
            //string Path = "file.csv";
            string dErrors = "," + "dError" + 0;
            for (int d = 1; d < _derivatives; d++)
                dErrors += "," + "dError" + d;

            string WriteNow = "rawX" + "," + "rawY" + "," + "predX" + "," + "predY" + "," + "rawVel" + "," + "error" + dErrors + "\n";
            System.IO.File.WriteAllText(Path, WriteNow);
            _logPath = Path;

            while (!System.IO.File.Exists(_logPath))
                System.Threading.Thread.Sleep(100);
            //System.Threading.Thread.Sleep(10000);

            _startLog = true;
            return;
        }
        private void FilterWriteLog()
        {
            if (_reportsAfterPenLift < 4 * _tabletHz || !_startLog)
                return;
            //System.Console.WriteLine("output");
            //string Path = "file.csv";
            string Path = _logPath;
            long rawX = (long)_rawState[_t[0]].X;
            long rawY = (long)_rawState[_t[0]].Y;
            long predX = (long)_evolvedState[_t[0]].X;
            long predY = (long)_evolvedState[_t[0]].Y;
            long rawVel = (long)(_rawState[_t[0]] - _rawState[_t[1]]).Length();
            long error = (long)(_rawState[_t[0]] - _evolvedState[_t[0]]).Length();

            long[] errorPerD = new long[_derivatives];
            Vector2 tempVector = Vector2.Zero;
            for (int d = 0; d < _derivatives; d++)
            {
                tempVector += _bestD[_t[1], d];
                errorPerD[d] = (long)(_rawState[_t[0]] - tempVector).Length();
            }

            string dErrors = "," + errorPerD[0];
            for (int d = 1; d < _derivatives; d++)
                dErrors += "," + (errorPerD[d - 1] - errorPerD[d]);

            string WriteNow = rawX + "," + rawY + "," + predX + "," + predY + "," + rawVel + "," + error + dErrors + "\n";
            System.IO.File.AppendAllText(Path, WriteNow);
            return;
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;
        protected override void ConsumeState()
        {
            if (State is ITabletReport tabletReport)
            {
                if (_initialise)
                {
                    FilterInitialise();
                    _initialise = false;
                }

                //_ignoredDuplicate = false;

                if (_ignoreDuplicate) //Ignore duplicates when previous cursor movement was faster than something, mb need to make it cleaner still
                    if (FilterCheckDuplicate(tabletReport.Position))
                    {
                        tabletReport.Position = _outputPosition;
                        OnEmit();
                        _ignoredDuplicate = true;
                        goto End; //Skip to end of thing
                    }
                    else
                        _ignoredDuplicate = false;

                _rawPosition = tabletReport.Position;
                _pressure = tabletReport.Pressure;

                if (_stateReset)
                {
                    _stateReset = false;
                    FilterStateReset(_rawPosition);
                }
                else
                {
                    FilterStateUpdate(_rawPosition);

                    _runCalc = true;
                }

                if (_interpolation == false) //not implemented
                {
                    _smoothTransition = 0f;
                    _outputPosition = FilterInterpolate(_reportDelta * _timeOffset);
                    tabletReport.Position = _outputPosition;
                    tabletReport.Pressure = _pressure;
                    OnEmit();
                }
            End: //
                ;
            }
            else
            {
                OnEmit();
            }
        }

        protected override void UpdateState()
        {
            float ReportWatchElapsed = (float)_reportWatch.Elapsed.TotalMilliseconds;

            if (State is ITabletReport tabletReport && ReportWatchElapsed < 1.2f * (_reportDelta + 2f * _leftoverDeltaMax - _leftoverDeltaMin))
            {
                if (_interpolation)
                {
                    _outputPosition = FilterInterpolate((float)_reportWatch.Elapsed.TotalMilliseconds);
                    tabletReport.Position = _outputPosition;
                    tabletReport.Pressure = _pressure;
                    OnEmit();
                }

                if (_runCalc)
                {
                    FilterStateUpdatePrediction();

                    if (logging)
                        FilterWriteLog();

                    _runCalc = false;


                    float TimeTaken = (float)_reportWatch.Elapsed.TotalMilliseconds - ReportWatchElapsed;
                    float Weight = 0.05f * (TimeTaken / (1f / (Frequency * (1 + _duplicate))));
                    if (Weight > 0.25f) Weight = 0.25f;
                    else if (Weight < 0.001f) Weight = 0.001f;
                    _statCalcTime = TimeTaken * Weight + (1f - Weight) * _statCalcTime;
                }

                if (_duplicate > 0 && _interpolation)
                    for (int d = 1; d <= _duplicate; d++)
                    {
                        double Time_Target = ((double)d / (double)(_duplicate + 1)) * (1000d / (double)Frequency) + (double)ReportWatchElapsed;

                        while (_reportWatch.Elapsed.TotalMilliseconds < Time_Target)
                            if (_reportWatch.Elapsed.TotalMilliseconds >= Time_Target)
                                break;

                        _outputPosition = FilterInterpolate((float)_reportWatch.Elapsed.TotalMilliseconds);
                        tabletReport.Position = _outputPosition;
                        tabletReport.Pressure = _pressure;
                        OnEmit();
                    }
            }
            else if (_stateReset != true)
            {
                _reportWatch.Stop();
                _stateReset = true;
                if (_weightAveraging && _ignoredDuplicate == false && _reportsAfterPenLift > 60 * _tabletHz) //just doing this rarely
                    FilterNormaliseError();

                if (_reportsAfterPenLift > 10 * _tabletHz && _writingSave == false)
                {
                    _writingSave = true;
                    System.Threading.Thread writeSaveFile = new System.Threading.Thread(() => WriteSave());
                    writeSaveFile.Start();
                }

                if (_ignoredDuplicate == false)
                {
                    //FilterNormaliseError();
                    System.Console.WriteLine("Calculation time: " + _statCalcTime / (1000f / Frequency) * (1 + _duplicate)
                                           + ". If higher than or close to 0.5, reduce weights or derivatives or duplicating value.\n"
                                           + "Try setting core affinity to last cores & priority high, too. It can have a massive effect.\n"
                                           + "\n"
                                           + "Prediction deviation: \n"
                                           + "error:" + _finalError[0] + "\n"
                                           + "stat error pred: " + _finalError[1]
                                           );

                    if (false) //scratchpad
                    {
                        int[] _deviation = new int[32];
                        string[] savesave = new string[_deviation.Length];

                        foreach (int i in _deviation)
                            savesave[i] = _deviation[i].ToString();

                        string Path = "file.csv";
                        string WriteNow = (long)_rawState[_t[0]].X + "," + (long)_rawState[_t[0]].Y + "," + (long)_evolvedState[_t[0]].X + "," + (long)_evolvedState[_t[0]].Y + "\n";
                        System.IO.File.AppendAllText(Path, WriteNow);
                    }
                }
            }
        }
    }
}
